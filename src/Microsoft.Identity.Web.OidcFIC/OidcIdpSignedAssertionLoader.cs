// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Microsoft.Identity.Web.OidcFic
{
    internal partial class OidcIdpSignedAssertionLoader : ICustomSignedAssertionProvider
    {
        private readonly ILogger<OidcIdpSignedAssertionLoader> _logger;
        private readonly IOptionsMonitor<MicrosoftIdentityApplicationOptions> _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;

        public OidcIdpSignedAssertionLoader(ILogger<OidcIdpSignedAssertionLoader> logger,
            IOptionsMonitor<MicrosoftIdentityApplicationOptions> options,
            IServiceProvider serviceProvider,
            ITokenAcquirerFactory tokenAcquirerFactory)
        {
            _logger = logger;
            _options = options;
            _serviceProvider = serviceProvider;
            _tokenAcquirerFactory = tokenAcquirerFactory;
        }

        public CredentialSource CredentialSource => CredentialSource.CustomSignedAssertion;

        public string Name => "OidcIdpSignedAssertion";

        internal static partial class Logger
        {
#if NET6_0_OR_GREATER
            /// <summary>
            /// Logger for when IConfiguration is not registered in the service collection.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="troubleshootingLink">Link to troubleshooting documentation.</param>
            [LoggerMessageAttribute(EventId = 500, Level = LogLevel.Error, Message = "[MsIdWeb] IConfiguration is not registered in the service collection. Please register IConfiguration or see {TroubleshootingLink} for more information.")]
            public static partial void ConfigurationNotRegistered(ILogger logger, string troubleshootingLink);

            /// <summary>
            /// Logger for binding configuration section to MicrosoftIdentityApplicationOptions.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="sectionName">Name of the configuration section.</param>
            [LoggerMessageAttribute(EventId = 501, Level = LogLevel.Debug, Message = "[MsIdWeb] Binding configuration section '{SectionName}' to MicrosoftIdentityApplicationOptions")]
            public static partial void ConfigurationBinding(ILogger logger, string sectionName);

            /// <summary>
            /// Logger for when CustomSignedAssertionProviderData is null.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            [LoggerMessageAttribute(EventId = 502, Level = LogLevel.Error, Message = "[MsIdWeb] CustomSignedAssertionProviderData is null")]
            public static partial void CustomSignedAssertionProviderDataNull(ILogger logger);

            /// <summary>
            /// Logger for when ConfigurationSection is null.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            [LoggerMessageAttribute(EventId = 503, Level = LogLevel.Error, Message = "[MsIdWeb] ConfigurationSection is null")]
            public static partial void ConfigurationSectionNull(ILogger logger);

            /// <summary>
            /// Logger for when signed assertion provider fails.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="providerName">Name of the provider.</param>
            /// <param name="message">Exception message.</param>
            [LoggerMessageAttribute(EventId = 504, Level = LogLevel.Error, Message = "[MsIdWeb] Failed to get signed assertion from {ProviderName}. Exception occurred: {Message}. Setting skip to true.")]
            public static partial void SignedAssertionProviderFailed(ILogger logger, string providerName, string message);
#else
            // Fallback implementation for older target frameworks
            private static readonly Action<ILogger, string, Exception?> s_configurationNotRegistered =
                LoggerMessage.Define<string>(
                    LogLevel.Error,
                    new EventId(500, "OidcIdpConfigurationNotRegistered"),
                    "[MsIdWeb] IConfiguration is not registered in the service collection. Please register IConfiguration or see {TroubleshootingLink} for more information.");

            private static readonly Action<ILogger, string, Exception?> s_configurationBinding =
                LoggerMessage.Define<string>(
                    LogLevel.Debug,
                    new EventId(501, "OidcIdpConfigurationBinding"),
                    "[MsIdWeb] Binding configuration section '{SectionName}' to MicrosoftIdentityApplicationOptions");

            private static readonly Action<ILogger, Exception?> s_customSignedAssertionProviderDataNull =
                LoggerMessage.Define(
                    LogLevel.Error,
                    new EventId(502, "OidcIdpCustomSignedAssertionProviderDataNull"),
                    "[MsIdWeb] CustomSignedAssertionProviderData is null");

            private static readonly Action<ILogger, Exception?> s_configurationSectionNull =
                LoggerMessage.Define(
                    LogLevel.Error,
                    new EventId(503, "OidcIdpConfigurationSectionNull"),
                    "[MsIdWeb] ConfigurationSection is null");

            private static readonly Action<ILogger, string, string, Exception?> s_signedAssertionProviderFailed =
                LoggerMessage.Define<string, string>(
                    LogLevel.Error,
                    new EventId(504, "OidcIdpSignedAssertionProviderFailed"),
                    "[MsIdWeb] Failed to get signed assertion from {ProviderName}. Exception occurred: {Message}. Setting skip to true.");

            /// <summary>
            /// Logger for when IConfiguration is not registered in the service collection.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="troubleshootingLink">Link to troubleshooting documentation.</param>
            public static void ConfigurationNotRegistered(
                ILogger logger,
                string troubleshootingLink) => s_configurationNotRegistered(logger, troubleshootingLink, default!);

            /// <summary>
            /// Logger for binding configuration section to MicrosoftIdentityApplicationOptions.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="sectionName">Name of the configuration section.</param>
            public static void ConfigurationBinding(
                ILogger logger,
                string sectionName) => s_configurationBinding(logger, sectionName, default!);

            /// <summary>
            /// Logger for when CustomSignedAssertionProviderData is null.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            public static void CustomSignedAssertionProviderDataNull(
                ILogger logger) => s_customSignedAssertionProviderDataNull(logger, default!);

            /// <summary>
            /// Logger for when ConfigurationSection is null.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            public static void ConfigurationSectionNull(
                ILogger logger) => s_configurationSectionNull(logger, default!);

            /// <summary>
            /// Logger for when signed assertion provider fails.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="providerName">Name of the provider.</param>
            /// <param name="message">Exception message.</param>
            public static void SignedAssertionProviderFailed(
                ILogger logger,
                string providerName,
                string message) => s_signedAssertionProviderFailed(logger, providerName, message, default!);
#endif
        }


        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters = null)
        {
            OidcIdpSignedAssertionProvider? signedAssertion = credentialDescription.CachedValue as OidcIdpSignedAssertionProvider;
            if (credentialDescription.CachedValue == null)
            {
                if (credentialDescription.CustomSignedAssertionProviderData == null)
                {
                    Logger.CustomSignedAssertionProviderDataNull(_logger);
                    throw new InvalidOperationException("CustomSignedAssertionProviderData is null");
                }

                string? sectionName = credentialDescription.CustomSignedAssertionProviderData["ConfigurationSection"] as string;
                if (sectionName == null)
                {
                    Logger.ConfigurationSectionNull(_logger);
                    throw new InvalidOperationException("ConfigurationSection is null");
                }

                MicrosoftIdentityApplicationOptions microsoftIdentityApplicationOptions = _options.Get(sectionName);
                
                if (string.IsNullOrEmpty(microsoftIdentityApplicationOptions.Instance) && microsoftIdentityApplicationOptions.Authority == "//v2.0")
                {
                    // Get IConfiguration from service provider just-in-time
                    IConfiguration? configuration = _serviceProvider.GetService<IConfiguration>();
                    if (configuration == null)
                    {
                        const string troubleshootingLink = "https://aka.ms/ms-id-web/fic-oidc/troubleshoot";
                        Logger.ConfigurationNotRegistered(_logger, troubleshootingLink);
                        throw new InvalidOperationException("IConfiguration is not registered in the service collection. " +
                            $"Please register IConfiguration in the service collection or configure MicrosoftIdentityOptions with named options '{sectionName}'. see https://aka.ms/ms-id-web/fic-oidc/troubleshoot for more information.");
                    }
                    
                    Logger.ConfigurationBinding(_logger, sectionName);
                    configuration.GetSection(sectionName).Bind(microsoftIdentityApplicationOptions);
                }

                // Special case for Signed assertions with an FmiPath.
                // The provider needs to postpone getting the signed assertion until the first call, when ClientAssertionFmiPath will be provided.
                signedAssertion = new OidcIdpSignedAssertionProvider(_tokenAcquirerFactory, microsoftIdentityApplicationOptions, credentialDescription.TokenExchangeUrl, _logger);
                if (credentialDescription.CustomSignedAssertionProviderData.TryGetValue("RequiresSignedAssertionFmiPath", out object? requiresSignedAssertionFmiPathObj) && requiresSignedAssertionFmiPathObj is bool requiresSignedAssertionFmiPathBool && requiresSignedAssertionFmiPathBool)
                {
                    signedAssertion.RequiresSignedAssertionFmiPath = true;
                }
            }

            try
            {
                // Try to get a signed assertion, and if it fails, move to the next credentials
                _ = await signedAssertion!.GetSignedAssertionAsync(null);
                credentialDescription.CachedValue = signedAssertion;
            }
            catch (Exception ex)
            {
                Logger.SignedAssertionProviderFailed(_logger, credentialDescription.CustomSignedAssertionProviderName ?? "Unknown", ex.Message);
                credentialDescription.Skip = true;
                throw;
            }
        }
    }
}
