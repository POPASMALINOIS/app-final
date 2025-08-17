using System;
using System.IO;

namespace OperativaLogistica.Services
{
    public static class AppPaths
    {
        public static readonly string Base =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "APP OPERATIVAS");
        public static readonly string Pdfs = Path.Combine(Base, "pdfs");
        public static readonly string Autosaves = Path.Combine(Base, "autosaves");
        public static readonly string Backups = Path.Combine(Base, "backups");
        public static readonly string MappingJson = Path.Combine(Base, "mapping.json");
        public static readonly string ColorsJson = Path.Combine(Base, "colors.json");
        public static readonly string ColumnLayoutJson = Path.Combine(Base, "column-layout.json");

        public static void Ensure()
        {
            Directory.CreateDirectory(Base);
            Directory.CreateDirectory(Pdfs);
            Directory.CreateDirectory(Autosaves);
            Directory.CreateDirectory(Backups);
        }
    }
}
