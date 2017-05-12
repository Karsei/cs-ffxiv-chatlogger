using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ffxiv_chatlogger
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            /*string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var item in resourceNames)
            {
                //MessageBox.Show(item);
            }
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                Assembly thisAssembly = Assembly.GetExecutingAssembly();
                var name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";
                //if (name.Equals("MahApps.Metro.resources.dll"))
                //    name = "MahApps.Metro.dll";
                MessageBox.Show(args.Name);
                var resources = thisAssembly.GetManifestResourceNames().Where(s => s.EndsWith(name));

                if (resources.Count() > 0)
                {
                    string resourceName = resources.First();
                    using (System.IO.Stream stream = thisAssembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            byte[] assembly = new byte[stream.Length];
                            stream.Read(assembly, 0, assembly.Length);
                            return Assembly.Load(assembly);
                        }
                    }
                }
                return null;
            };*/
            App.Main();
        }
    }
}
