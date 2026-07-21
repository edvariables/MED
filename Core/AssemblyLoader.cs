using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Xml.Linq;

namespace MED
{
    public class AssemblyLoader : AssemblyLoadContext
    {
        private string folderPath;

        public AssemblyLoader(string folderPath)
        {
            this.folderPath = folderPath;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var deps = DependencyContext.Default;
            var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
            if (res.Count > 0)
            {
                return Assembly.Load(new AssemblyName(res.First().Name));
            }
            else
            {
                var apiApplicationFileInfo = new FileInfo($"{folderPath}{Path.DirectorySeparatorChar}{assemblyName.Name}.dll");
                if (File.Exists(apiApplicationFileInfo.FullName))
                {
                    return this.LoadFromAssemblyPath(apiApplicationFileInfo.FullName);
                }
            }
            return Assembly.Load(assemblyName);
        }

        public static object CreateObjectInstance(string processLib, string processClass, object[] paramsObjects)
        {
            foreach(var ass in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in ass.GetTypes())
                    if (type.FullName == processClass)
                    {
                        return Activator.CreateInstance(type, paramsObjects, []);
                    }



            var processType = Type.GetType(processClass);
            if (processType != null)
                return Activator.CreateInstance(processType.Namespace, processType.Name, paramsObjects);

            //TODO
            var al = new AssemblyLoader(Directory.GetParent(processLib).FullName);

            Assembly assembly = al.LoadFromAssemblyPath(processLib);
            
            return Activator.CreateInstance(assembly.GetName().Name, processClass, paramsObjects);

            foreach (var type in assembly.GetExportedTypes())
                if (type.FullName == processClass)
                {
                    return Activator.CreateInstance(assembly.GetName().Name, processClass, paramsObjects);
                }
            return null;
        }
    }
}
