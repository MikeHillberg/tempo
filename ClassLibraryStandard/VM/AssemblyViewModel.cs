using System;
using System.Collections.Generic;
using System.Text;

namespace Tempo
{
    public class AssemblyLocation
    {
        public AssemblyLocation(string path)
        {
            Path = path;
        }

        public AssemblyLocation(string path, string containerPath)
        {
            Path = path;
            ContainerPath = containerPath;
        }

        // Path in filesystem or within nupkg
        public string Path { get; }

        // Nupkg location
        public string ContainerPath { get; }

        public override string ToString()
        {
            if(ContainerPath == null)
            {
                return Path;
            }

            return $"{Path} ({ContainerPath})";
        }
    }
}
