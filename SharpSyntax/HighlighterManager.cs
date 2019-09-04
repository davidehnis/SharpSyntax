using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace SharpSyntax
{
    public class HighlighterManager
    {
        private HighlighterManager()
        {
            Highlighters = new Dictionary<string, IHighlighter>();

            var gfdgd = Application.GetResourceStream(new Uri("pack://application:,,,/SharpSyntax;component/resources/syntax.xsd"));
            if (gfdgd == null) return;
            var schemaStream = gfdgd.Stream;
            var schema = XmlSchema.Read(schemaStream, (s, e) =>
            {
                Debug.WriteLine("Xml schema validation error : " + e.Message);
            });

            var readerSettings = new XmlReaderSettings();
            readerSettings.Schemas.Add(schema);
            readerSettings.ValidationType = ValidationType.Schema;

            foreach (var res in GetResources("resources/(.+?)[.]xml"))
            {
                XDocument xmldoc;
                try
                {
                    var reader = XmlReader.Create(res.Value, readerSettings);
                    xmldoc = XDocument.Load(reader);
                }
                catch (XmlSchemaValidationException ex)
                {
                    Debug.WriteLine("Xml validation error at line " + ex.LineNumber + " for " + res.Key + " :");
                    Debug.WriteLine("Warning : if you cannot find the issue in the xml file, verify the xsd file.");
                    Debug.WriteLine(ex.Message);
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return;
                }

                var root = xmldoc.Root;
                var name = root?.Attribute("name")?.Value.Trim();
                if (name is null) return;
                Highlighters.Add(name, new XmlHighlighter(root));
            }
        }

        public static HighlighterManager Instance { get; } = new HighlighterManager();

        public IDictionary<string, IHighlighter> Highlighters { get; private set; }

        private IDictionary<string, UnmanagedMemoryStream> GetResources(string filter)
        {
            var asm = Assembly.GetCallingAssembly();
            var resName = asm.GetName().Name + ".g.resources";
            var manifestStream = asm.GetManifestResourceStream(resName);

            IDictionary<string, UnmanagedMemoryStream> ret = new Dictionary<string, UnmanagedMemoryStream>();
            if (manifestStream is null) return ret;
            var reader = new ResourceReader(manifestStream);
            foreach (DictionaryEntry res in reader)
            {
                var path = (string)res.Key;
                var stream = (UnmanagedMemoryStream)res.Value;
                if (Regex.IsMatch(path, filter))
                    ret.Add(path, stream);
            }

            return ret;
        }
    }
}