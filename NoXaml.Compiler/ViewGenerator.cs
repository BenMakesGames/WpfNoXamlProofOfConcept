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

            return implementedInterfaces.Any(i => i.ToString() == "NoXaml.Interfaces.Components.INoXaml");
        }

        private string GenerateViewCode(ViewMeta view)
        {
            var lastPeriod = view.ClassFilePath.LastIndexOf('.');
            var xmlFilePath = view.ClassFilePath.Substring(0, lastPeriod) + ".xml";

            if (!File.Exists(xmlFilePath))
                return null;

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            var buildUICode = new StringBuilder();

            var rootNodes = xmlDoc.ChildNodes.Cast<XmlNode>().ToList();

            var processingInstructions = rootNodes
                .Where(n => n is XmlProcessingInstruction)
                .Cast<XmlProcessingInstruction>()
            ;

            var usingStatements = GenerateUsingStatements(processingInstructions);

            if (rootNodes.FirstOrDefault(n => n is XmlElement) is not XmlElement root)
                return null;

            AddNode(root, buildUICode, 0);

            string classCode = $@"
using NoXaml.Framework.Extensions.WPF;
using NoXaml.Interfaces.Components;
using System.Windows;
using System.Windows.Controls;
{usingStatements}

namespace {view.Namespace}
{{
    public partial class {view.ClassName}: INoXaml
    {{
        public override void BuildUI()
        {{
{buildUICode}
        }}
    }}
}}";

            return classCode;
        }

        private string Indent(int level)
        {
            return new string('\t', level + 3);
        }

        private int nextVarIndex = 1;

        private string GetTempVarName()
        {
            var varName = $"_v{nextVarIndex}";
            nextVarIndex++;

            return varName;
        }

        private void AddNode(XmlElement node, StringBuilder code, int level)
        {
            var nodeType = node.Name;

            if (nodeType.StartsWith("#"))
                return;

            var attributes = node.Attributes.Cast<XmlAttribute>().ToList();
            var depth = 1;

            if (level == 0)
                code.AppendLine($"{Indent(level)}Content = (new {nodeType}()");
            else
            {
                if(GetForEach(attributes) is ForEachAttribute fe)
                {
                    var varName = GetTempVarName();

                    code.AppendLine($"{Indent(level)}.ForEach({fe.Collection}, ({varName}, {fe.ItemVariableName}) =>");
                    code.AppendLine($"{Indent(level + 1)}{varName}.Add(new {nodeType}()");

                    depth++;
                }
                else if(GetIf(attributes) is IfAttribute i)
                {
                    var varName = GetTempVarName();

                    code.AppendLine($"{Indent(level)}.If({i.Condition}, {varName} =>");
                    code.AppendLine($"{Indent(level + 1)}{varName}.Add(new {nodeType}()");

                    depth++;
                }
                else
                    code.AppendLine($"{Indent(level)}.Add(new {nodeType}()");
            }

            foreach (var attr in attributes)
                AddAttribute(attr, code, level + depth);

            var children = node.ChildNodes.Cast<XmlNode>().Where(n => n is XmlElement).Cast<XmlElement>();

            foreach (var child in children)
                AddNode(child, code, level + depth);

            if (level == 0)
                code.AppendLine($"{Indent(level)}{new string(')', depth)};");
            else
                code.AppendLine($"{Indent(level)}{new string(')', depth)}");
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

        private void AddAttribute(XmlAttribute attr, StringBuilder code, int level)
        {
            switch(attr.Name.ToLower())
            {
                case "_click":
                    code.AppendLine($"{Indent(level)}.OnClick((_sender, _args) => {attr.Value})");
                    break;
                case "text":
                    code.AppendLine($@"{Indent(level)}.SetText({attr.Value.ToLiteral()})");
                    break;
                case "_text":
                    code.AppendLine($@"{Indent(level)}.SetText({attr.Value})");
                    break;
            }
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
