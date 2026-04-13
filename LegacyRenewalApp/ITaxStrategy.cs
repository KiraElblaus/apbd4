namespace LegacyRenewalApp;

public interface ITaxStrategy
{
    bool AppliesTo(string country);
    decimal GetRate();
}

public class PolandTaxStrategy : ITaxStrategy
{
    public bool AppliesTo(string country) => country == "Poland";
    public decimal GetRate() => 0.23m;
}

public class GermanyTaxStrategy : ITaxStrategy
{
    public bool AppliesTo(string country) => country == "Germany";
    public decimal GetRate() => 0.19m;
}

public class CzechRepublicTaxStrategy : ITaxStrategy
{
    public bool AppliesTo(string country) => country == "Czech Republic";
    public decimal GetRate() => 0.21m;
    
}

public class NorwayTaxStrategy : ITaxStrategy
{
    public bool AppliesTo(string country) => country == "Norway";
    public decimal GetRate() => 0.25m;
}

public class DefaultTaxStrategy : ITaxStrategy
{
    public bool AppliesTo(string country) => true;
    public decimal GetRate() => 0.20m;
}