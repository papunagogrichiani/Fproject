using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NLog;

class Program
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public class Card
    {
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; } // MM/YY
        public string Pin { get; set; }
        public decimal Balance { get; set; }
        public decimal EuroBalance { get; set; } // Euro balance
        public decimal DollarBalance { get; set; } // Dollar balance
        public List<string> TransactionHistory { get; set; } = new List<string>();
        public string Cvc { get; set; } // CVC code
    }

    static void Main()
    {
        while (true)
        {
            logger.Info("Application started");

            var card = LoadCardData();
            if (ValidateCard(card))
            {
                if (ValidatePin(card))
                {
                    MainMenu(card);
                    break; // Program exits after main menu interaction
                }
                else
                {
                    Console.WriteLine("Invalid PIN. Exiting.");
                    logger.Warn("PIN validation failed");
                }
            }
            else
            {
                Console.WriteLine("Invalid card details. Restarting.");
                logger.Warn("Card validation failed. Restarting.");
            }
        }

        logger.Info("Application finished");
    }

    static Card LoadCardData()
    {
        try
        {
            string json = File.ReadAllText("C:\\Users\\papun\\source\\repos\\project\\cardData.json");
            logger.Info("Card data loaded");
            return JsonConvert.DeserializeObject<Card>(json);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error loading card data");
            throw;
        }
    }

    static void SaveCardData(Card card)
    {
        try
        {
            // Serialize the card data
            string json = JsonConvert.SerializeObject(card, Formatting.Indented);
            File.WriteAllText("C:\\Users\\papun\\source\\repos\\project\\cardData.json", json);
            logger.Info("Card data saved");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error saving card data");
            throw;
        }
    }

    static bool ValidateCard(Card card)
    {
        Console.Write("Enter card number: ");
        string cardNumber = Console.ReadLine();
        Console.Write("Enter expiry date (MM/YY): ");
        string expiryInput = Console.ReadLine();
        Console.Write("Enter CVC: ");
        string cvcInput = Console.ReadLine();

        // Validate the expiry date
        bool isValidDate = DateTime.TryParseExact(expiryInput, "MM/yy", null, System.Globalization.DateTimeStyles.None, out DateTime expiryDate);
        bool isValidCvc = cvcInput.Length == 3 && int.TryParse(cvcInput, out _);
        bool isValid = card.CardNumber == cardNumber &&
                       isValidDate &&
                       DateTime.ParseExact(card.ExpiryDate, "MM/yy", null) >= DateTime.Now &&
                       card.Cvc == cvcInput &&
                       isValidCvc;

        logger.Info($"Card validation {(isValid ? "successful" : "failed")}");
        return isValid;
    }

    static bool ValidatePin(Card card)
    {
        Console.Write("Enter PIN: ");
        string pin = Console.ReadLine();
        bool isValid = card.Pin == pin;
        logger.Info($"PIN validation {(isValid ? "successful" : "failed")}");
        return isValid;
    }

    static void MainMenu(Card card)
    {
        while (true)
        {
            Console.WriteLine("\n1. View Balance\n2. Withdraw Money\n3. View Last 5 Transactions\n4. Deposit Money\n5. Change PIN\n6. Convert Currency\n7. Exit");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ViewBalance(card);
                    SaveCardData(card); // Save after each action
                    break;
                case "2":
                    WithdrawMoney(card);
                    break;
                case "3":
                    ViewLastTransactions(card);
                    break;
                case "4":
                    DepositMoney(card);
                    break;
                case "5":
                    ChangePin(card);
                    break;
                case "6":
                    ConvertCurrency(card);
                    break;
                case "7":
                    logger.Info("User exited the system");
                    return;
                default:
                    Console.WriteLine("Invalid choice. Try again.");
                    logger.Warn("Invalid menu option selected");
                    break;
            }
        }
    }

    static void WithdrawMoney(Card card)
    {
        Console.WriteLine("Select currency to withdraw:");
        Console.WriteLine("1. GEL\n2. EUR\n3. USD");
        string choice = Console.ReadLine();

        decimal amount = 0;

        switch (choice)
        {
            case "1":
                Console.Write("Enter amount to withdraw in GEL: ");
                amount = decimal.Parse(Console.ReadLine());

                if (amount <= card.Balance)
                {
                    card.Balance -= amount;
                    Console.WriteLine($"You withdrew {amount} GEL. New balance: {card.Balance} GEL.");
                    LogTransaction(card, $"Withdrew {amount} GEL");
                }
                else
                {
                    Console.WriteLine("Insufficient balance.");
                    logger.Warn("Withdrawal failed due to insufficient balance");
                }
                break;

            case "2":
                Console.Write("Enter amount to withdraw in EUR: ");
                amount = decimal.Parse(Console.ReadLine());

                if (amount <= card.EuroBalance)
                {
                    card.EuroBalance -= amount;
                    Console.WriteLine($"You withdrew {amount} EUR. New balance: {card.EuroBalance} EUR.");
                    LogTransaction(card, $"Withdrew {amount} EUR");
                }
                else
                {
                    Console.WriteLine("Insufficient Euro balance.");
                    logger.Warn("Euro withdrawal failed due to insufficient balance");
                }
                break;

            case "3":
                Console.Write("Enter amount to withdraw in USD: ");
                amount = decimal.Parse(Console.ReadLine());

                if (amount <= card.DollarBalance)
                {
                    card.DollarBalance -= amount;
                    Console.WriteLine($"You withdrew {amount} USD. New balance: {card.DollarBalance} USD.");
                    LogTransaction(card, $"Withdrew {amount} USD");
                }
                else
                {
                    Console.WriteLine("Insufficient Dollar balance.");
                    logger.Warn("Dollar withdrawal failed due to insufficient balance");
                }
                break;

            default:
                Console.WriteLine("Invalid choice.");
                break;
        }

        SaveCardData(card); // Save after withdrawal
    }

    static void ViewLastTransactions(Card card)
    {
        Console.WriteLine("Last 5 transactions:");
        foreach (var transaction in card.TransactionHistory.TakeLast(5))
        {
            Console.WriteLine(transaction);
        }
        logger.Info("Viewed last 5 transactions");
    }

    static void DepositMoney(Card card)
    {
        Console.Write("Enter amount to deposit in GEL: ");
        decimal amount = decimal.Parse(Console.ReadLine());

        card.Balance += amount;
        Console.WriteLine($"You deposited {amount} GEL. New balance: {card.Balance} GEL.");
        LogTransaction(card, $"Deposited {amount} GEL");
        SaveCardData(card); // Save after deposit
    }

    static void ChangePin(Card card)
    {
        Console.Write("Enter new PIN: ");
        string newPin = Console.ReadLine();

        card.Pin = newPin;
        Console.WriteLine("PIN changed successfully.");
        LogTransaction(card, "Changed PIN");
        SaveCardData(card); // Save after changing PIN
    }

    static void LogTransaction(Card card, string action)
    {
        card.TransactionHistory.Add($"{DateTime.Now}: {action}");

        // Ensure only the last 5 transactions are kept
        if (card.TransactionHistory.Count > 5)
        {
            card.TransactionHistory.RemoveAt(0); // Remove the oldest transaction
        }

        logger.Info(action);
    }

    static void ViewBalance(Card card)
    {
        Console.WriteLine($"Your balance: {card.Balance} GEL");
        Console.WriteLine($"Your balance: {card.EuroBalance:F2} EUR");
        Console.WriteLine($"Your balance: {card.DollarBalance:F2} USD");
        LogTransaction(card, "Viewed balance");
    }

    static void ConvertCurrency(Card card)
    {
        Console.WriteLine("Select currency to convert to:");
        Console.WriteLine("1. Euro\n2. US Dollar");
        string choice = Console.ReadLine();

        Console.Write("Enter amount in GEL: ");
        decimal amount = decimal.Parse(Console.ReadLine());

        decimal convertedAmount = 0;

        switch (choice)
        {
            case "1":
                convertedAmount = amount * 0.34m; // Example conversion rate for Euro
                card.EuroBalance += convertedAmount; // Update Euro balance
                Console.WriteLine($"{amount} GEL is approximately {convertedAmount:F2} EUR.");
                break;
            case "2":
                convertedAmount = amount * 0.37m; // Example conversion rate for US Dollar
                card.DollarBalance += convertedAmount; // Update Dollar balance
                Console.WriteLine($"{amount} GEL is approximately {convertedAmount:F2} USD.");
                break;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }

        SaveCardData(card); // Save after conversion
    }
}
