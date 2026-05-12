using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class FieldDefinitionProjections
{
    /// <summary>
    /// Expression projection from FieldDefinition to FieldDefinitionDto. Used by
    /// <see cref="FieldDefinitions.List"/> so EF emits a focused SELECT instead of materializing
    /// the entity then mapping in C# memory. The FieldDefinitionType static lookup
    /// (Name + DisplayName) is resolved post-materialize via FieldDefinitionType.AllLookupDictionary
    /// — never .Include() static lookup bindings, per the project pattern.
    /// </summary>
    public static readonly Expression<Func<FieldDefinition, FieldDefinitionDto>> AsDto = fd => new FieldDefinitionDto
    {
        FieldDefinitionID = fd.FieldDefinitionID,
        FieldDefinitionValue = fd.FieldDefinitionValue,
        FieldDefinitionType = new FieldDefinitionTypeSimpleDto
        {
            FieldDefinitionTypeID = fd.FieldDefinitionTypeID,
            // Name/DisplayName populated post-materialize from FieldDefinitionType.AllLookupDictionary.
        },
    };
}
