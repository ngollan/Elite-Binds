using System;
using System.Xml;
using System.Xml.Schema;
using System.IO;

namespace EliteBinds {
  
  // This appears to be the sanest way to fudge a default namespace into an
  // XML reader.
  class BindsNamespaceManager : XmlNamespaceManager {
    public override string DefaultNamespace { get => "urn:EliteDangerousBinds.3.0.xsd"; }

    public BindsNamespaceManager() : base(new NameTable()) {
    }

    public override string LookupNamespace (string prefix) {
      if (prefix == "") {
        return DefaultNamespace;
      } else {
        return base.LookupNamespace(prefix);
      }
    }
  }

  class Program {
    private const string SchemaFilename = "binds.xsd";
    private static string BindingsPath = Path.Combine(
      Environment.GetEnvironmentVariable("LOCALAPPDATA"),
      "Frontier Developments", "Elite Dangerous", "Options", "Bindings"
    );

    static void Main(string[] args) {
      XmlNamespaceManager nsmgr = new BindsNamespaceManager();
      XmlReaderSettings settings = new XmlReaderSettings();

      settings.Schemas.Add(
        null,
        System.AppDomain.CurrentDomain.BaseDirectory + SchemaFilename
      );

      settings.ValidationType = ValidationType.Schema;
      settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
      settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
      settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

      XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

      if (nsmgr.HasNamespace(String.Empty)) Console.WriteLine(nsmgr.DefaultNamespace);

      if (args.Length == 0) {
        args = Directory.GetFiles(BindingsPath, "*.binds");
      }

      foreach(string filename in args) {
        Console.WriteLine(filename);

        XmlReader reader = XmlReader.Create(
          inputUri: filename,
          settings: settings,
          inputContext: context
        );

        while (reader.Read()) ;
      }

    }

    // Display any warnings or errors.
    private static void ValidationCallBack(object sender, ValidationEventArgs args) {
      var lineInfo = "";

      try {
        var r = sender as IXmlLineInfo;
        lineInfo = $"[{r.LineNumber}, {r.LinePosition}] ";
      } catch { }

      if (args.Severity == XmlSeverityType.Warning) {
        Console.WriteLine($"\t{lineInfo}Warning: Matching schema not found. No validation occurred. {args.Message}");
      } else {
        Console.WriteLine($"\t{lineInfo}Validation error: {args.Message}");
      }
    }
  }
}
