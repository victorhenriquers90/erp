using Microsoft.Extensions.DependencyInjection;

namespace ProjetoVarejo.Desktop;

public static class ScopedFormHelper
{
    public static void AbrirModal<T>(IWin32Window? owner = null) where T : Form
    {
        using var scope = Program.Services.CreateScope();
        var form = scope.ServiceProvider.GetRequiredService<T>();
        form.ShowDialog(owner);
    }
}
