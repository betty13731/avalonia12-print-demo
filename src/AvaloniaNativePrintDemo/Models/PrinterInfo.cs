namespace AvaloniaNativePrintDemo.Models;

public record PrinterInfo(string Name, bool IsDefault)
{
    public override string ToString() => IsDefault ? $"{Name} (по умолчанию)" : Name;
}
