using System;
using System.Collections.Generic;
using System.Linq;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
       
        private readonly CustomerRepository _customerRepository;
        private readonly SubscriptionPlanRepository _planRepository;
        private readonly IBillingGateway _billingGateway;
        private readonly DiscountCalculator _discountCalculator;
        private readonly IEnumerable<IPaymentFeeStrategy> _paymentStrategies;
        private readonly IEnumerable<ITaxStrategy> _taxStrategies;
        
        public SubscriptionRenewalService() : this(
            new CustomerRepository(), 
            new SubscriptionPlanRepository(), 
            new BillingGatewayWrapper(),
            new DiscountCalculator(),
            new List<IPaymentFeeStrategy> { new CardPaymentStrategy(), new BankTransferPaymentStrategy(), new PayPalPaymentStrategy(), new InvoicePaymentStrategy() },
            new List<ITaxStrategy> { new PolandTaxStrategy(), new GermanyTaxStrategy(), new CzechRepublicTaxStrategy(), new NorwayTaxStrategy(), new DefaultTaxStrategy() }
        ) { }
        
        public SubscriptionRenewalService(
            CustomerRepository customerRepository,
            SubscriptionPlanRepository planRepository,
            IBillingGateway billingGateway,
            DiscountCalculator discountCalculator,
            IEnumerable<IPaymentFeeStrategy> paymentStrategies,
            IEnumerable<ITaxStrategy> taxStrategies)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _billingGateway = billingGateway;
            _discountCalculator = discountCalculator;
            _paymentStrategies = paymentStrategies;
            _taxStrategies = taxStrategies;
        }
        
        public RenewalInvoice CreateRenewalInvoice(
            int customerId, string planCode, int seatCount, string paymentMethod, 
            bool includePremiumSupport, bool useLoyaltyPoints)
        {
            ValidateInputs(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();
            
            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
            string notes = string.Empty;
            
            var discountResult = _discountCalculator.Calculate(customer, plan, seatCount, baseAmount, useLoyaltyPoints);
            decimal discountAmount = discountResult.Amount;
            notes += discountResult.Notes;

            decimal subtotalAfterDiscount = Math.Max(baseAmount - discountAmount, 300m);
            if (subtotalAfterDiscount == 300m && (baseAmount - discountAmount) < 300m)
                notes += "minimum discounted subtotal applied; ";
            
            decimal supportFee = CalculateSupportFee(includePremiumSupport, normalizedPlanCode, ref notes);
            
            var paymentStrategy = _paymentStrategies.FirstOrDefault(s => s.AppliesTo(normalizedPaymentMethod)) 
                ?? throw new ArgumentException("Unsupported payment method");
            
            decimal paymentFee = paymentStrategy.CalculateFee(subtotalAfterDiscount + supportFee);
            notes += paymentStrategy.GetNote();
            
            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            var taxStrategy = _taxStrategies.FirstOrDefault(s => s.AppliesTo(customer.Country)) ?? new DefaultTaxStrategy();
            decimal taxAmount = taxBase * taxStrategy.GetRate();
            
            decimal finalAmount = Math.Max(taxBase + taxAmount, 500m);
            if (finalAmount == 500m && (taxBase + taxAmount) < 500m)
                notes += "minimum invoice amount applied; ";
            
            var invoice = BuildInvoice(customer, normalizedPlanCode, seatCount, normalizedPaymentMethod, 
                baseAmount, discountAmount, supportFee, paymentFee, taxAmount, finalAmount, notes);
            
            _billingGateway.SaveInvoice(invoice);
            
            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                _billingGateway.SendEmail(
                    customer.Email, 
                    "Subscription renewal invoice", 
                    $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} has been prepared. Final amount: {invoice.FinalAmount:F2}.");
            }

            return invoice;
        }
        
        private void ValidateInputs(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            if (customerId <= 0) throw new ArgumentException("Customer id must be positive");
            if (string.IsNullOrWhiteSpace(planCode)) throw new ArgumentException("Plan code is required");
            if (seatCount <= 0) throw new ArgumentException("Seat count must be positive");
            if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method is required");
        }

        private decimal CalculateSupportFee(bool includePremiumSupport, string planCode, ref string notes)
        {
            if (!includePremiumSupport) return 0m;
            notes += "premium support included; ";
            return planCode switch
            {
                "START" => 250m,
                "PRO" => 400m,
                "ENTERPRISE" => 700m,
                _ => 0m
            };
        }

        private RenewalInvoice BuildInvoice(Customer customer, string planCode, int seatCount, string paymentMethod,
            decimal baseAmount, decimal discountAmount, decimal supportFee, decimal paymentFee, decimal taxAmount, decimal finalAmount, string notes)
        {
            return new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customer.Id}-{planCode}",
                CustomerName = customer.FullName,
                PlanCode = planCode,
                PaymentMethod = paymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
}