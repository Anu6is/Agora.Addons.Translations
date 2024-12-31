using Agora.Addons.Disqord;
using Agora.Shared.Attributes;
using Agora.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Agora.Addons.Translations;

[AgoraService(Scope = AgoraServiceAttribute.ServiceLifetime.Scoped)]
public class GlobalizationResources(ILogger<GlobalizationResources> logger) : AgoraService(logger), ILocalizationService, IDisposable
{
    private readonly ConcurrentDictionary<string, ResourceManager> _resourceManagers = new();

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("en-Us");

    private ResourceManager GetResourceManager(string baseResourceName) => _resourceManagers.GetOrAdd(baseResourceName, resourceName =>
    {
        return new ResourceManager($"{typeof(GlobalizationResources).Namespace}.Resources.{resourceName}", Assembly.GetExecutingAssembly());
    });

    public string Translate(string key, string resourceName)
    {
            try
            {
                var resourceManager = GetResourceManager(resourceName);
                var translation = resourceManager.GetString(key, CurrentCulture);

                if (!string.IsNullOrEmpty(translation))
                {
                    return translation;
                }
            }
            catch (Exception) { }
        
        return key;
    }

    public void SetCulture(CultureInfo culture)
    {
        CurrentCulture = culture ?? CultureInfo.GetCultureInfo("en-Us");
    }

    public class ResourceGroup
    {
        private readonly GlobalizationResources _parent;
        private readonly string _resourceName;
        private readonly ConcurrentDictionary<string, string> _cachedTranslations;

        internal ResourceGroup(GlobalizationResources parent, string resourceName)
        {
            _parent = parent;
            _resourceName = resourceName;
            _cachedTranslations = new ConcurrentDictionary<string, string>();
        }

        public string this[string key] => GetTranslation(key);

        public string GetTranslation(string key) => _cachedTranslations.GetOrAdd(key, k => _parent.Translate(k, _resourceName));

        public Dictionary<string, string> TranslateMultiple(params string[] keys) => keys.ToDictionary(key => key, GetTranslation);

        public class Typed<T> where T : struct
        {
            private readonly ResourceGroup _parent;
            private readonly Func<string, T> _converter;

            internal Typed(ResourceGroup parent, Func<string, T> converter)
            {
                _parent = parent;
                _converter = converter;
            }

            public T Get(string key)
            {
                string translation = _parent.GetTranslation(key);
                return _converter(translation);
            }
        }

        public Typed<int> AsIntegers() => new(this, int.Parse);

        public Typed<double> AsDoubles() => new(this, double.Parse);

        public Typed<bool> AsBooleans() =>  new(this, bool.Parse);
    }

    public ResourceGroup CreateGroup(string resourceName)
    {
        return new ResourceGroup(this, resourceName);
    }

    public void Dispose()
    {
        foreach (var resourceManager in _resourceManagers.Values)
        {
            resourceManager.ReleaseAllResources();
        }
    }
}
