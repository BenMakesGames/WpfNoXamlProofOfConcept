using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NoXaml.Compiler
{
    [Generator]
    public class ViewGenerator: ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //Debugger.Launch();

            try
            {
                var views = GetViews(context.Compilation);

                foreach (var view in views)
                {
                    var code = GenerateViewCode(view);

                    if (code != null)
                    {
                        var id = view.Namespace.Replace(".", "") + view.ClassName + "NoXaml";
                        context.AddSource(id, code);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                if(!Debugger.IsAttached)
                    Debugger.Launch();
            }
        }

        private ImmutableArray<ViewMeta> GetViews(Compilation compilation)
        {
            return compilation
                .SyntaxTrees
                .SelectMany(s => s.GetRoot().DescendantNodes())
                .Where(n => n.IsKind(SyntaxKind.NamespaceDeclaration))
                .OfType<NamespaceDeclarationSyntax>()
                .SelectMany(n => n.DescendantNodes()
                    .Where(c => c.IsKind(SyntaxKind.ClassDeclaration))
                    .OfType<ClassDeclarationSyntax>()
                    .Select(c => new
                    {
                        Namespace = n,
                        Class = c
                    })
                )
                .Where(c => IsView(compilation, c.Class))
                .Select(c => CreateViewMeta(c.Namespace, c.Class))
                .ToImmutableArray()
            ;
        }

        private ViewMeta CreateViewMeta(NamespaceDeclarationSyntax n, ClassDeclarationSyntax c)
        {
            var namespaceName = n.Name.ToString();

            return new ViewMeta()
            {
                Namespace = namespaceName,
                ClassName = c.Identifier.Text,
                ClassFilePath = c.Identifier.GetLocation().SourceTree.FilePath,
            };
        }

        private bool IsView(Compilation compilation, ClassDeclarationSyntax c)
        {
            var tree = c.SyntaxTree;
            var root = tree.GetRoot();
            var sModel = compilation.GetSemanticModel(c.SyntaxTree);
            var classSymbol = sModel.GetDeclaredSymbol(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First());

            var implementedInterfaces = classSymbol.AllInterfaces;

            return implementedInterfaces.Any(i => i.ToString() == "NoXaml.Model.Components.INoXaml");
        }

        private string GenerateViewCode(ViewMeta view)
        {
            var lastPeriod = view.ClassFilePath.LastIndexOf('.');
            var xmlFilePath = view.ClassFilePath.Substring(0, lastPeriod) + ".xml";

            if (!File.Exists(xmlFilePath))
                return null;

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            var buildVDOMCode = new StringBuilder();

            var rootNodes = xmlDoc.ChildNodes.Cast<XmlNode>().ToList();

            var processingInstructions = rootNodes
                .Where(n => n is XmlProcessingInstruction)
                .Cast<XmlProcessingInstruction>()
            ;

            var usingStatements = GenerateUsingStatements(processingInstructions);

            if (rootNodes.FirstOrDefault(n => n is XmlElement) is not XmlElement root)
                return null;

            AddNode(root, buildVDOMCode, "_0");

            string classCode = $@"
using NoXaml.Framework.Extensions.WPF;
using NoXaml.Model.Components;
using NoXaml.Model.DOM;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
{usingStatements}

namespace {view.Namespace}
{{
    public partial class {view.ClassName}: INoXaml
    {{
        public override Element BuildVDOM()
        {{
{buildVDOMCode}
            return _0;
        }}
    }}
}}";

            return classCode;
        }

        private string Indent(int level)
        {
            return new string('\t', level + 3);
        }

        private readonly static string[] ReservedProperties = new[]
        {
            "_if",
            "_foreach"
        };

        private void AddNode(XmlElement node, StringBuilder code, string nodeVarName, int indentLevel = 0)
        {
            var nodeType = node.Name;

            if (nodeType.StartsWith("#"))
                return;

            var attributes = node.Attributes.Cast<XmlAttribute>().ToList();

            code.AppendLine($"{Indent(indentLevel)}var {nodeVarName} = new Element(typeof({nodeType}));");

            var componentProperties = attributes
                .Where(a => !ReservedProperties.Contains(a.Name.ToLower()))
                .ToList()
            ;

            if(componentProperties.Count > 0)
            {
                code.AppendLine($"{Indent(indentLevel)}{nodeVarName}.Properties = new Dictionary<string, object>()");
                code.AppendLine($"{Indent(indentLevel)}{{");

                foreach (var attr in componentProperties)
                {
                    var propertyName = attr.Name;
                    var value = attr.Value;

                    if (propertyName.StartsWith("_"))
                    {
                        propertyName = propertyName.Substring(1);

                        if (propertyName.ToLower() == "click") // TODO: detect event properties
                        {
                            value = $"(RoutedEventHandler)((_sender, _args) => {value})";
                        }
                    }
                    else
                    {
                        value = value.ToLiteral();
                    }

                    code.AppendLine($"{Indent(indentLevel + 1)}{{ {propertyName.ToLiteral()}, {value} }},");
                }

                code.AppendLine($"{Indent(indentLevel)}}};");
            }

            var children = node.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement).Cast<XmlElement>();
            var childIndex = 1;

            foreach (var child in children)
            {
                var childAttributes = child.Attributes.Cast<XmlAttribute>().ToList();
                var childVarName = $"{nodeVarName}Child{childIndex}";

                if (GetIf(childAttributes) is IfAttribute i)
                {
                    code.AppendLine($"{Indent(indentLevel)}if({i.Condition})");
                    code.AppendLine($"{Indent(indentLevel)}{{");

                    AddNode(child, code, childVarName, indentLevel + 1);

                    code.AppendLine($"{Indent(indentLevel + 1)}{nodeVarName}.Children.Add({childVarName});");
                    code.AppendLine($"{Indent(indentLevel)}}}");
                }
                else if (GetForEach(childAttributes) is ForEachAttribute fe)
                {
                    code.AppendLine($"{Indent(indentLevel)}{nodeVarName}.Children.AddRange({fe.Collection}.Select({fe.ItemVariableName} => {{");
                    
                    AddNode(child, code, childVarName + "ForEach", indentLevel + 1);
                    
                    code.AppendLine($"{Indent(indentLevel + 1)}return {childVarName}ForEach;");
                    code.AppendLine($"{Indent(indentLevel)}}}));");
                }
                else
                {
                    AddNode(child, code, childVarName, indentLevel);

                    code.AppendLine($"{Indent(0)}{nodeVarName}.Children.Add({childVarName});");
                }

                childIndex++;
            }
        }

        private ForEachAttribute GetForEach(List<XmlAttribute> attrs)
        {
            var forEachAttr = attrs.FirstOrDefault(a => a.Name.ToLower() == "_foreach");

            if (forEachAttr == null)
                return null;

            var match = Regex.Match(forEachAttr.Value, "([^ ]+) +(of|in) +(.+)");

            if (!match.Success)
                return null; // TODO throw exception

            return new ForEachAttribute()
            {
                Collection = match.Groups[3].Value,
                ItemVariableName = match.Groups[1].Value
            };
        }

        private IfAttribute GetIf(List<XmlAttribute> attrs)
        {
            var ifAttr = attrs.FirstOrDefault(a => a.Name.ToLower() == "_if");

            if (ifAttr == null)
                return null;

            return new IfAttribute()
            {
                Condition = ifAttr.Value.Trim()
            };
        }

        private string GenerateUsingStatements(IEnumerable<XmlProcessingInstruction> processingInstructions)
        {
            var usingStatements = "";

            foreach(var i in processingInstructions)
            {
                if (i.Name.ToLower() == "using")
                    usingStatements += $"using {i.Value.Trim()};{Environment.NewLine}";
            }

            return usingStatements;
        }
    }
}
