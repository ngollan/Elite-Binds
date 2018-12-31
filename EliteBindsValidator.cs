using System;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace EliteBinds {

  // This appears to be the sanest way to fudge a default namespace into an
  // XML reader.
  class BindsNamespaceManager : XmlNamespaceManager {
    public override string DefaultNamespace { get => FileInfo.BindsSchemaNamespace; }

    public BindsNamespaceManager() : base(new NameTable()) {
    }

    public override string LookupNamespace (string prefix) {
      if (prefix == String.Empty) {
        return DefaultNamespace;
      } else {
        return base.LookupNamespace(prefix);
      }
    }
  }

  // 
  class VersionInfo {
    public int Major {get; set;}
    public int Minor {get; set;}

    public VersionInfo() {
      Major = 0;
      Minor = 0;
    }
    public VersionInfo(int major, int minor) {
      Major = major;
      Minor = minor;
    }

    public override string ToString() => $"{Major}.{Minor}";

    public bool IsZero() => (Major == 0 && Minor == 0);
  }

  class ErrorInfo {
    public int Row {get; private set;}
    public int Column {get; private set;}
    public string Message {get; private set;}

    // Build instance from schema validation arguments.
    public ErrorInfo(object sender, ValidationEventArgs args) {
      Row = Column = -1;

      try {
        var r = sender as IXmlLineInfo;
        Row = r.LineNumber;
        Column = r.LinePosition;
      } catch { }

      Message = args.Message;
    }

    public override string ToString() {
      if (Row > 0 && Column > 0) {
        return $"[line {Row}, character {Column}] {Message}";
      } else {
        return Message;
      }
    }
  }

  // Contains various information about a binds file.
  class FileInfo {
    public static string BindsSchemaNamespace = "urn:EliteDangerousBinds.3.0.xsd";
    public const string SchemaResource = "Elite Binds.binds.xsd";
    public const string ProcessingSchemaResource = "Elite Binds.binds-processing.xsd";
    public string Path {get; private set;}
    public string FileName {get {return System.IO.Path.GetFileName(Path);} }
    public string PresetName {get; private set;}
    public VersionInfo Version {get; private set;}

    public List<ErrorInfo> SchemaErrors {get; private set;}

    public FileInfo(string path) {
      Path = path;

      // Initialise the preset name from the file name, assuming that's what's suposed to happen for premade binds.
      PresetName = System.IO.Path.GetFileNameWithoutExtension(Path);

      Version = new VersionInfo();
      SchemaErrors = new List<ErrorInfo>();
      Validate();
    }

    public override string ToString() => $"{PresetName} <{Version.ToString()}>";
    public string GeneratedFileName {
      get {
        if (Version.IsZero()) {
          return $"{PresetName}.binds";
        } else {
          return $"{PresetName}.{Version.ToString()}.binds";
        }
      }
    }

    public bool GeneratedFilenameMatch() {
      return GeneratedFileName == FileName;
    }

    public void Validate() {
      XmlNamespaceManager nsmgr = new BindsNamespaceManager();
      XmlReaderSettings settings = new XmlReaderSettings();

      var a = Assembly.GetEntryAssembly();
      settings.Schemas.Add(null, XmlReader.Create(a.GetManifestResourceStream(SchemaResource)));
      settings.Schemas.Add(null, XmlReader.Create(a.GetManifestResourceStream(ProcessingSchemaResource)));

      settings.ValidationType = ValidationType.Schema;
      settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
      settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
      settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);

      XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);

      if (nsmgr.HasNamespace(String.Empty)) Console.WriteLine(nsmgr.DefaultNamespace);

      XmlReader reader = XmlReader.Create(
        inputUri: Path,
        settings: settings,
        inputContext: context
      );

      SchemaErrors.Clear();

      while (reader.Read()) {
        if(reader.NodeType is XmlNodeType.Element) {
          var si = reader.SchemaInfo;
          var se = si.SchemaElement;

          if (se == null) {
            // The file probably stopped making sense, keep reading to emit errors and maybe get lucky.
            continue;
          }

          if (se.SchemaTypeName.Namespace == BindsSchemaNamespace) {
            switch (se.SchemaTypeName.Name) {
              case "bindsContainer":
                EvaluateRoot(reader);
                break;
            }
          }
        }
      }
    }

    // Read naming and version information from root element.
    private void EvaluateRoot(XmlReader reader) {
      if (reader.HasAttributes) {
        while (reader.MoveToNextAttribute()) {
          switch (reader.Name) {
            case "PresetName":
              PresetName = reader.Value;
              break;

            case "MajorVersion":
            case "MinorVersion":
              var v = Int32.Parse(reader.Value);

              if (reader.Name == "MajorVersion") {
                Version.Major = v;
              }
              else {
                Version.Minor = v;
              }

              break;
          }
        }

        reader.MoveToElement();
      }
    }

    private void ValidationCallback(object sender, ValidationEventArgs args) {
      SchemaErrors.Add(new ErrorInfo(sender, args));
    }
  }

  class Program {
    private static string BindingsPath = Path.Combine(
      Environment.GetEnvironmentVariable("LOCALAPPDATA"),
      "Frontier Developments", "Elite Dangerous", "Options", "Bindings"
    );

    static void Main(string[] args) {
      IEnumerable<string> filenames;

      if (args.Length == 0) {
        filenames = Directory.GetFiles(BindingsPath, "*.binds");
      } else {
        List<string[]> globbed = args.Select((path) => {
          var fn = System.IO.Path.GetFileName(path);
          var pn = System.IO.Path.GetDirectoryName(path);

          if (fn == String.Empty) {
            fn = "*.binds";
          }

          return Directory.GetFiles(pn, fn);
        }).ToList();
        filenames = new List<String>();

        foreach (string[] names in globbed) {
          foreach(string filename in names) {
            (filenames as List<string>).Add(filename);
          }
        }
      }

      // map preset name to file names
      var conflictInfo = new SortedDictionary<string, List<string>>();

      foreach(string filename in filenames) {
        var fi = new FileInfo(filename);
        Console.WriteLine(fi.FileName);

        if (!fi.GeneratedFilenameMatch()) {
          Console.WriteLine($"\tFile is named \"{fi.FileName}\" but should be \"{fi.GeneratedFileName}\"");
        }

        foreach(ErrorInfo error in fi.SchemaErrors) {
          Console.WriteLine("\t" + error);
        }

        List<string> names;
        conflictInfo.TryGetValue(fi.PresetName, out names);
        if (null == names) {
          names = new List<string>();
          conflictInfo.Add(fi.PresetName, names);
        }

        names.Add(fi.FileName);
      }

      foreach(KeyValuePair<string, List<string>> info in conflictInfo) {
        if (info.Value.Count <= 1) {
          continue;
        }

        Console.WriteLine($"Preset \"{info.Key}\" is defined in multiple files:");

        foreach(string fn in info.Value) {
          Console.WriteLine("\t" + fn);
        }
      }
    }
  }
}
