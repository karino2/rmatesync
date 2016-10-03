using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMateSync
{
    class Settings
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        string _iniPath;

        public Settings(FileInfo inifile)
        {
            _iniPath = inifile.FullName;
        }

        const string SECTION_NAME = "rmatesync";

        public void WriteString(String key, string value)
        {
            WritePrivateProfileString(SECTION_NAME, key, value, _iniPath);
        }

        public string ReadString(String key, string defValue)
        {
            var builder = new StringBuilder(1024);
            GetPrivateProfileString(SECTION_NAME, key, defValue, builder, 1024, _iniPath);
            return builder.ToString();
        }

        public string ReadEditorPath()
        {
            return ReadString("editor", "notepad.exe");
        }

        public string ReadEditorArgs()
        {
            return ReadString("editorargs", "");
        }


        public static Settings Create()
        {
            return new RMateSync.Settings(new FileInfo(Path.Combine(Environment.CurrentDirectory, "settings.ini")));
        }


    }
}
