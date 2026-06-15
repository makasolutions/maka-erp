using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace FSH.Modules.NamingSeries.Domain;

/// <summary>
/// Value object inmutable que representa un patrón de numeración (ADR-0007).
/// Patrón = secuencia ordenada de tokens parseados a partir de una cadena.
/// Tokens soportados:
///   - <c>{YYYY}</c> año 4 dígitos · <c>{YY}</c> año 2 dígitos
///   - <c>{MM}</c> mes 2 dígitos · <c>{DD}</c> día 2 dígitos
///   - <c>{#}+</c> contador con padding cero (N <c>#</c> = N dígitos)
///   - literal: cualquier carácter ASCII imprimible fuera de <c>{}</c>
/// Una serie DEBE contener exactamente UN bloque contador. Cero o más-de-uno → <see cref="InvalidPatternException"/>.
/// </summary>
public sealed class NamingPattern : IEquatable<NamingPattern>
{
    public string Raw { get; }
    public IReadOnlyList<PatternToken> Tokens { get; }
    public int CounterWidth { get; }

    private NamingPattern(string raw, IReadOnlyList<PatternToken> tokens, int counterWidth)
    {
        Raw = raw;
        Tokens = tokens;
        CounterWidth = counterWidth;
    }

    /// <summary>Parsea el patrón. Lanza <see cref="InvalidPatternException"/> si es inválido.</summary>
    public static NamingPattern Parse(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var tokens = new List<PatternToken>();
        int counterCount = 0;
        int counterWidth = 0;
        int i = 0;
        var literal = new StringBuilder();

        while (i < pattern.Length)
        {
            char c = pattern[i];
            if (c == '{')
            {
                if (literal.Length > 0)
                {
                    tokens.Add(new LiteralToken(literal.ToString()));
                    literal.Clear();
                }

                int end = pattern.IndexOf('}', i + 1);
                if (end < 0)
                {
                    throw new InvalidPatternException($"Bloque sin cerrar en patrón '{pattern}' (posición {i}). Falta '}}'.");
                }

                string inner = pattern.Substring(i + 1, end - i - 1);
                if (inner.Length == 0)
                {
                    throw new InvalidPatternException($"Bloque vacío '{{}}' en patrón '{pattern}' (posición {i}).");
                }

                PatternToken token = ParseInnerToken(inner, pattern);
                if (token is CounterToken counter)
                {
                    counterCount++;
                    counterWidth = counter.Width;
                }
                tokens.Add(token);
                i = end + 1;
                continue;
            }

            if (c == '}')
            {
                throw new InvalidPatternException($"'}}' sin '{{' previo en patrón '{pattern}' (posición {i}).");
            }

            literal.Append(c);
            i++;
        }

        if (literal.Length > 0)
        {
            tokens.Add(new LiteralToken(literal.ToString()));
        }

        if (counterCount == 0)
        {
            throw new InvalidPatternException($"Patrón '{pattern}' no contiene contador. Debe incluir exactamente un bloque '{{#}}+'.");
        }
        if (counterCount > 1)
        {
            throw new InvalidPatternException($"Patrón '{pattern}' contiene {counterCount} contadores. Solo se permite uno.");
        }

        return new NamingPattern(pattern, new ReadOnlyCollection<PatternToken>(tokens), counterWidth);
    }

    /// <summary>True si el patrón es válido — wrapper de <see cref="Parse"/>.</summary>
    public static bool IsValid(string pattern, out string? error)
    {
        try
        {
            _ = Parse(pattern);
            error = null;
            return true;
        }
        catch (InvalidPatternException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static PatternToken ParseInnerToken(string inner, string fullPattern)
    {
        // Counter token: ##...# (uno o más '#')
        if (inner.Length > 0 && inner[0] == '#')
        {
            foreach (char ch in inner)
            {
                if (ch != '#')
                {
                    throw new InvalidPatternException($"Bloque contador inválido '{{{inner}}}' en '{fullPattern}'. Solo se permiten '#'.");
                }
            }
            return new CounterToken(inner.Length);
        }

        return inner switch
        {
            "YYYY" => YearToken.FourDigit,
            "YY" => YearToken.TwoDigit,
            "MM" => MonthToken.Instance,
            "DD" => DayToken.Instance,
            _ => throw new InvalidPatternException($"Token desconocido '{{{inner}}}' en patrón '{fullPattern}'. Tokens válidos: {{YYYY}}, {{YY}}, {{MM}}, {{DD}}, {{#}}+."),
        };
    }

    /// <summary>
    /// Formatea el patrón sustituyendo tokens. El contador se renderiza con padding cero a
    /// <see cref="CounterWidth"/> dígitos; si <paramref name="counterValue"/> excede el ancho,
    /// mantiene la longitud real (DIAN exige unicidad, no padding fijo).
    /// </summary>
    public string Format(int counterValue, DateTimeOffset date)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(counterValue);

        var sb = new StringBuilder(Raw.Length + CounterWidth + 4);
        foreach (var token in Tokens)
        {
            switch (token)
            {
                case LiteralToken lit:
                    sb.Append(lit.Value);
                    break;
                case YearToken year:
                    sb.Append((year.Digits == 4 ? date.Year : date.Year % 100).ToString(year.Digits == 4 ? "D4" : "D2", CultureInfo.InvariantCulture));
                    break;
                case MonthToken:
                    sb.Append(date.Month.ToString("D2", CultureInfo.InvariantCulture));
                    break;
                case DayToken:
                    sb.Append(date.Day.ToString("D2", CultureInfo.InvariantCulture));
                    break;
                case CounterToken counter:
                    sb.Append(counterValue.ToString(CultureInfo.InvariantCulture).PadLeft(counter.Width, '0'));
                    break;
            }
        }
        return sb.ToString();
    }

    public bool Equals(NamingPattern? other) => other is not null && string.Equals(Raw, other.Raw, StringComparison.Ordinal);
    public override bool Equals(object? obj) => Equals(obj as NamingPattern);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Raw);
    public override string ToString() => Raw;
}

public abstract record PatternToken;
public sealed record LiteralToken(string Value) : PatternToken;
public sealed record YearToken(int Digits) : PatternToken
{
    public static readonly YearToken FourDigit = new(4);
    public static readonly YearToken TwoDigit = new(2);
}
public sealed record MonthToken : PatternToken
{
    public static readonly MonthToken Instance = new();
}
public sealed record DayToken : PatternToken
{
    public static readonly DayToken Instance = new();
}
public sealed record CounterToken(int Width) : PatternToken;
