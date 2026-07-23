using Azure.Extensions.AspNetCore.Configuration.Secrets;

namespace Neptune.Common
{
    /// <summary>
    /// Maps Key Vault secret names onto Neptune configuration keys. Neptune's config
    /// keys are flat PascalCase (e.g. "DatabaseConnectionString", "SendGridApiKey"),
    /// which are valid Key Vault secret names as-is, so no name transformation is
    /// needed and the base provider mapping ("Section--Key" -> "Section:Key") is kept
    /// unchanged. This named manager exists as the single, documented place to add a
    /// mapping override should a future secret key ever need one (mirrors wave-runup's
    /// WaveRunupKeyVaultSecretManager, which maps "-" -> "_" for its underscore keys).
    /// </summary>
    public class NeptuneKeyVaultSecretManager : KeyVaultSecretManager
    {
    }
}
