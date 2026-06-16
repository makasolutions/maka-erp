using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain;

namespace Parties.Tests.Domain;

/// <summary>PR-D4: reconciliación canónica código Tabla Básica v1 ↔ enum TipoIdentificacion v2.</summary>
public class IdentificationTypeMapperTests
{
    [Theory]
    [InlineData("CC", TipoIdentificacion.CC)]
    [InlineData("CE", TipoIdentificacion.CE)]
    [InlineData("NIT", TipoIdentificacion.NIT)]
    [InlineData("TI", TipoIdentificacion.TI)]
    [InlineData("NUIP", TipoIdentificacion.NUIP)]
    [InlineData("PASAPORTE", TipoIdentificacion.Pasaporte)]
    [InlineData("NIT_EXT", TipoIdentificacion.NITExtranjero)]
    public void Shared_Codes_RoundTrip(string code, TipoIdentificacion expected)
    {
        IdentificationTypeMapper.TryToEnum(code).ShouldBe(expected);
        IdentificationTypeMapper.ToCode(expected).ShouldBe(code);
        // ida y vuelta
        IdentificationTypeMapper.TryToEnum(IdentificationTypeMapper.ToCode(expected)).ShouldBe(expected);
    }

    [Fact]
    public void EnumOnly_Rut_Pep_MapToForwardCodes()
    {
        IdentificationTypeMapper.ToCode(TipoIdentificacion.RUT).ShouldBe("RUT");
        IdentificationTypeMapper.ToCode(TipoIdentificacion.PEP).ShouldBe("PEP");
        // simétrico
        IdentificationTypeMapper.TryToEnum("RUT").ShouldBe(TipoIdentificacion.RUT);
        IdentificationTypeMapper.TryToEnum("PEP").ShouldBe(TipoIdentificacion.PEP);
    }

    [Fact]
    public void ToCode_IsTotal_ForAllEnumValues()
    {
        foreach (var tipo in Enum.GetValues<TipoIdentificacion>())
        {
            var code = IdentificationTypeMapper.ToCode(tipo);
            code.ShouldNotBeNullOrWhiteSpace();
            IdentificationTypeMapper.TryToEnum(code).ShouldBe(tipo); // reversible
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("CODIGO_DESCONOCIDO")]
    public void TryToEnum_DirtyInput_ReturnsNull(string? code)
    {
        IdentificationTypeMapper.TryToEnum(code).ShouldBeNull();
    }

    [Fact]
    public void TryToEnum_IsCaseInsensitive()
    {
        IdentificationTypeMapper.TryToEnum("nit").ShouldBe(TipoIdentificacion.NIT);
        IdentificationTypeMapper.TryToEnum("Cc").ShouldBe(TipoIdentificacion.CC);
    }
}
