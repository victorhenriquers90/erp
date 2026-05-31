namespace ProjetoVarejo.DesktopShell;

public sealed class ShellSettings
{
    public string Url { get; set; } = "http://127.0.0.1:5094/";
    public bool StartApi { get; set; } = true;
    public string ApiExeRelativePath { get; set; } = @"..\web\ProjetoVarejo.Api.exe";
    public int StartupTimeoutSeconds { get; set; } = 90;
}
