namespace LegacyRenewalApp;

public interface IPaymentFeeStrategy
{
    bool AppliesTo(string paymentMethod);
    decimal CalculateFee(decimal amount);
    string GetNote();

}

public class CardPaymentStrategy : IPaymentFeeStrategy
{
    public bool AppliesTo(string paymentMethod) => paymentMethod == "CARD";
    public decimal CalculateFee(decimal amount) => amount * 0.23m;
    public string GetNote() => "Card payment fee: ";
}

public class BankTransferPaymentStrategy : IPaymentFeeStrategy
{
    public bool AppliesTo(string paymentMethod) => paymentMethod == "BANK_TRANSFER";
    public decimal CalculateFee(decimal amount) => amount * 0.01m;
    public string GetNote() => "Bank transfer fee: ";
}

public class PayPalPaymentStrategy : IPaymentFeeStrategy
{
    public bool AppliesTo(string paymentMethod) => paymentMethod == "PAYPAL";
    public decimal CalculateFee(decimal amount) => amount * 0.035m;
    public string GetNote() => "PayPal fee: ";
}

public class InvoicePaymentStrategy : IPaymentFeeStrategy
{
    public bool AppliesTo(string paymentMethod) => paymentMethod == "INVOICE";
    public decimal CalculateFee(decimal amount) => 0m;
    public string GetNote() => "Invoice payment: ";
}