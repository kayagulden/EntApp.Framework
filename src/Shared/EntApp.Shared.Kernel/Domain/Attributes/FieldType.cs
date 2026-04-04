namespace EntApp.Shared.Kernel.Domain.Attributes;

/// <summary>
/// Dynamic UI field tipi.
/// Frontend'te hangi component'in render edileceğini belirler.
/// Attribute'ta belirtilmezse CLR tipinden otomatik türetilir.
/// </summary>
public enum FieldType
{
    /// <summary>Otomatik türet (CLR tipinden).</summary>
    Auto = 0,

    /// <summary>Tek satır metin — Input.</summary>
    String,

    /// <summary>Çok satırlı metin — Textarea.</summary>
    Text,

    /// <summary>Tam sayı — Input type="number".</summary>
    Number,

    /// <summary>Ondalıklı sayı — Input + format.</summary>
    Decimal,

    /// <summary>Para birimi — Input + currency prefix.</summary>
    Money,

    /// <summary>Tarih — DatePicker.</summary>
    Date,

    /// <summary>Tarih + saat — DateTimePicker.</summary>
    DateTime,

    /// <summary>Boolean — Switch/Toggle.</summary>
    Boolean,

    /// <summary>Enum — Select/Dropdown.</summary>
    Enum,

    /// <summary>İlişkili entity — Async Combobox (lookup).</summary>
    Lookup,

    /// <summary>Dosya yükleme — FileUpload.</summary>
    File,

    /// <summary>Zengin metin editörü — RichTextEditor.</summary>
    RichText
}
