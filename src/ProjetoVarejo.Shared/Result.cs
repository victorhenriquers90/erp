namespace ProjetoVarejo.Shared;

public class Result
{
    public bool Sucesso { get; }
    public string? Erro { get; }

    protected Result(bool sucesso, string? erro)
    {
        Sucesso = sucesso;
        Erro = erro;
    }

    public static Result Ok() => new(true, null);
    public static Result Falha(string erro) => new(false, erro);

    public static Result<T> Ok<T>(T valor) => Result<T>.Ok(valor);
    public static Result<T> Falha<T>(string erro) => Result<T>.Falha(erro);
}

public class Result<T> : Result
{
    public T? Valor { get; }

    private Result(bool sucesso, T? valor, string? erro) : base(sucesso, erro)
    {
        Valor = valor;
    }

    public static Result<T> Ok(T valor) => new(true, valor, null);
    public new static Result<T> Falha(string erro) => new(false, default, erro);
}
