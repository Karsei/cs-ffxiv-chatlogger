using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;

namespace ffxiv_chatlogger
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        private readonly StreamWriter m_out;

        public App()
        {
            /*AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var resourceName = Assembly.GetExecutingAssembly().GetName().Name + ".DllsAsResource." + new AssemblyName(args.Name).Name + ".dll";
                using (System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        var assemblyData = new byte[stream.Length];
                        stream.Read(assemblyData, 0, assemblyData.Length);
                        return Assembly.Load(assemblyData);
                    }
                }
                return null;

                Assembly thisAssembly = Assembly.GetExecutingAssembly();
                var name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";
                if (name.Equals("MahApps.Metro.resources.dll"))
                    name = "MahApps.Metro.dll";
                //MessageBox.Show(name);
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

            // stdout을 파일로 출력
            Console.SetOut(m_out = new StreamWriter("chat.log", true, Encoding.UTF8) { AutoFlush = true });
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            this.m_out.Flush();
            this.m_out.Close();
        }

        // .NET 4.0 이상
        /*private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            var name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";
            var resources = thisAssembly.GetManifestResourceNames().Where(s => s.EndsWith(name));

            if (resources.Count() > 0)
            {
                string resourceName = resources.First();
                using (Stream stream = thisAssembly.GetManifestResourceStream(resourceName))
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
        }*/

        // LINQ 지원 X
        /*private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string resourceName = null;
            string fileName = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";

            foreach (string name in thisAssembly.GetManifestResourceNames())
            {
                if (name.EndsWith(fileName))
                {
                    resourceName = name;
                }
            }

            if (resourceName != null)
            {
                using (Stream stream = thisAssembly.GetManifestResourceStream(resourceName))
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
        }*/
    }
}
