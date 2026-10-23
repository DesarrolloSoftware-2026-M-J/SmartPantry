using SmartPantry.EntityFrameworkCore;
using Xunit;

namespace SmartPantry.ExpirationWarnings;

[Collection(SmartPantryTestConsts.CollectionDefinitionName)]
public class EfCoreExpirationWarningProcessorTests
    : ExpirationWarningProcessor_Tests<SmartPantryEntityFrameworkCoreTestModule>
{
}