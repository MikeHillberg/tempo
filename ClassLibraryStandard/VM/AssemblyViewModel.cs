using MiddleweightReflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tempo
{
    /// <summary>
    /// ViewModel wrapper around MrAssembly, exposing assembly-level metadata.
    /// </summary>
    public class AssemblyViewModel
    {
        readonly MrAssembly _assembly;
        readonly TypeSet _typeSet;

        public AssemblyViewModel(MrAssembly assembly, TypeSet typeSet = null)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _typeSet = typeSet;
        }

        /// <summary>
        /// The underlying MrAssembly.
        /// </summary>
        public MrAssembly Assembly => _assembly;

        /// <summary>
        /// Assembly name from the metadata (via AssemblyDefinition).
        /// </summary>
        public string Name => _assembly.Name;

        /// <summary>
        /// Full assembly name including version, culture, and public key token.
        /// E.g. "MyAssembly, Version=1.2.3.0, Culture=neutral, PublicKeyToken=null"
        /// </summary>
        public string FullName => _assembly.GetAssemblyName()?.FullName;

        /// <summary>
        /// Assembly version from the metadata.
        /// </summary>
        public Version Version => _assembly.Version;

        /// <summary>
        /// Version as a display string.
        /// </summary>
        public string VersionString
        {
            get
            {
                var v = _assembly.Version;
                if (v == null) return "";
                // WinRT WinMDs use 255.255.255.255 as a placeholder
                if (v.Major == 255 && v.Minor == 255 && v.Build == 255 && v.Revision == 255) return "";
                return v.ToString();
            }
        }

        /// <summary>
        /// Culture string, or empty for culture-neutral assemblies.
        /// </summary>
        public string Culture => _assembly.Culture ?? string.Empty;

        /// <summary>
        /// Culture display string, showing "neutral" for culture-neutral assemblies.
        /// </summary>
        public string CultureDisplay => string.IsNullOrEmpty(_assembly.Culture) ? "" : _assembly.Culture;

        /// <summary>
        /// File path of the loaded assembly.
        /// </summary>
        public string Location => _assembly.Location;

        /// <summary>
        /// True if this is a fake (unresolved) assembly.
        /// </summary>
        public bool IsFakeAssembly => _assembly.IsFakeAssembly;

        /// <summary>
        /// Assembly flags (e.g. Retargetable).
        /// </summary>
        public AssemblyFlags Flags => _assembly.Flags;

        /// <summary>
        /// Hash algorithm used by the assembly.
        /// </summary>
        public string HashAlgorithm => _assembly.HashAlgorithm.ToString();

        /// <summary>
        /// Module Version ID (MVID) — a GUID that uniquely identifies this build.
        /// </summary>
        public Guid ModelVersionId => _assembly.Mvid;

        /// <summary>
        /// MVID as a display string, or empty if not available.
        /// </summary>
        public string ModelVersionIdString
        {
            get
            {
                var mvid = _assembly.Mvid;
                return mvid == Guid.Empty ? "" : mvid.ToString();
            }
        }

        /// <summary>
        /// Module name (typically the filename).
        /// </summary>
        public string ModuleName => _assembly.ModuleName;

        /// <summary>
        /// Whether the assembly is strong-named.
        /// </summary>
        public bool IsStrongNamed => !_assembly.PublicKey.IsEmpty;

        /// <summary>
        /// Public key token as a hex string, or null if not strong-named.
        /// </summary>
        public string PublicKeyToken
        {
            get
            {
                var token = _assembly.GetAssemblyName()?.GetPublicKeyToken();
                if (token == null || token.Length == 0)
                    return null;
                return BitConverter.ToString(token).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Names and versions of referenced assemblies.
        /// </summary>
        IList<AssemblyName> _referencedAssemblies;
        public IList<AssemblyName> ReferencedAssemblies
        {
            get
            {
                if (_referencedAssemblies == null)
                {
                    _referencedAssemblies = _assembly.GetReferencedAssemblies()
                                                     .OrderBy(r => r.Name)
                                                     .ToList();
                }
                return _referencedAssemblies;
            }
        }

        /// <summary>
        /// Assembly-level custom attributes.
        /// </summary>
        IList<CustomAttributeViewModel> _customAttributes;
        public IList<CustomAttributeViewModel> CustomAttributes
        {
            get
            {
                if (_customAttributes == null)
                {
                    try
                    {
                        _customAttributes = _assembly.GetCustomAttributes()
                            .Select(a => (CustomAttributeViewModel)new MRCustomAttributeViewModel(a, _typeSet))
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        UnhandledExceptionManager.ProcessException(ex);
                        _customAttributes = new List<CustomAttributeViewModel>();
                    }
                }
                return _customAttributes;
            }
        }

        /// <summary>
        /// Target framework from the TargetFrameworkAttribute, if present.
        /// E.g. ".NETCoreApp,Version=v8.0"
        /// </summary>
        string _targetFramework;
        bool _targetFrameworkChecked;
        public string TargetFramework
        {
            get
            {
                if (!_targetFrameworkChecked)
                {
                    _targetFrameworkChecked = true;
                    try
                    {
                        foreach (var attr in CustomAttributes)
                        {
                            if (attr.Name == "TargetFrameworkAttribute")
                            {
                                if (attr.ConstructorArguments != null && attr.ConstructorArguments.Count > 0)
                                {
                                    _targetFramework = attr.ConstructorArguments[0].Value as string;
                                }
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    { 
                        UnhandledExceptionManager.ProcessException(ex);
                    }
                }
                return _targetFramework;
            }
        }

        public override string ToString()
        {
            return $"AssemblyViewModel: {Name}";
        }
    }
}
