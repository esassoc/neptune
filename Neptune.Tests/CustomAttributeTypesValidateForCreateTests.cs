using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.Tests
{
    /// <summary>
    /// Covers the NPT-1038 rework guard that blocks creation of Modeling-purpose
    /// Custom Attribute Types via the admin editor. The SPA filters the Purpose
    /// dropdown to hide Modeling on create, and this validator is the backend
    /// belt-and-suspenders.
    /// </summary>
    [TestClass]
    public class CustomAttributeTypesValidateForCreateTests
    {
        [TestMethod]
        public void Validate_NullDto_ReturnsError()
        {
            var result = CustomAttributeTypes.ValidateForCreate(null);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Validate_ModelingPurpose_ReturnsError()
        {
            var dto = new CustomAttributeTypeUpsertDto
            {
                CustomAttributeTypePurposeID = (int)CustomAttributeTypePurposeEnum.Modeling,
            };

            var result = CustomAttributeTypes.ValidateForCreate(dto);

            Assert.IsNotNull(result);
            StringAssert.Contains(result, "Modeling");
        }

        [TestMethod]
        public void Validate_OtherDesignPurpose_Succeeds()
        {
            var dto = new CustomAttributeTypeUpsertDto
            {
                CustomAttributeTypePurposeID = (int)CustomAttributeTypePurposeEnum.OtherDesignAttributes,
            };

            Assert.IsNull(CustomAttributeTypes.ValidateForCreate(dto));
        }

        [TestMethod]
        public void Validate_MaintenancePurpose_Succeeds()
        {
            var dto = new CustomAttributeTypeUpsertDto
            {
                CustomAttributeTypePurposeID = (int)CustomAttributeTypePurposeEnum.Maintenance,
            };

            Assert.IsNull(CustomAttributeTypes.ValidateForCreate(dto));
        }
    }
}
