using System;
using System.Threading;

class BankAccount
{
    private int balance;
    private Mutex _mutex = new Mutex();  // Mutex for thread-safe balance access

    public Mutex GetMutex()
    {
        return _mutex;
    }
    public BankAccount(int initialBalance)
    {
        balance = initialBalance;
    }

    public int GetBalance()
    {
        _mutex.WaitOne();
        try
        {
            return balance;
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }

    public void SetBalance(int amount)
    {
        _mutex.WaitOne();
        try
        {
            balance = amount;
            Console.WriteLine($"Balance manually updated to {balance}");
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }

    public void Deposit(int amount)
{
    Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Waiting to deposit {amount}...");
    _mutex.WaitOne();  // Lock access to balance
    try
    {
        Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Depositing {amount}...");
        balance += amount;
        Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] New balance: {balance}");
    }
    finally
    {
        _mutex.ReleaseMutex();
    }
}

public void Withdraw(int amount)
{
    Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Waiting to withdraw {amount}...");
    _mutex.WaitOne();
    try
    {
        if (balance >= amount)
        {
            Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Withdrawing {amount}...");
            balance -= amount;
            Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] New balance: {balance}");
        }
        else
        {
            Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Insufficient funds! Balance: {balance}");
        }
    }
    finally
    {
        _mutex.ReleaseMutex();
    }
}

}

class Program
{
    static void Transfer(BankAccount from, BankAccount to, int amount)
    {
        Console.WriteLine($"Attempting to transfer {amount}...");

        bool fromLocked = false, toLocked = false;

        try
        {
            fromLocked = from.GetMutex().WaitOne(1000); // Try acquiring first lock
            if (!fromLocked)
            {
                Console.WriteLine("Failed to lock first account, retrying...");
                return;
            }

            Thread.Sleep(100);  // Simulate delay to potentially cause deadlock

            toLocked = to.GetMutex().WaitOne(1000); // Try acquiring second lock
            if (!toLocked)
            {
                Console.WriteLine("Failed to lock second account, retrying...");
                return;
            }

            // Critical section
            if (from.GetBalance() >= amount)
            {
                from.SetBalance(from.GetBalance() - amount);
                to.SetBalance(to.GetBalance() + amount);
                Console.WriteLine($"Transferred {amount}. New balances - From: {from.GetBalance()}, To: {to.GetBalance()}");
            }
            else
            {
                Console.WriteLine($"Transfer failed due to insufficient funds. From balance: {from.GetBalance()}");
            }
        }
        finally
        {
            if (fromLocked) from.GetMutex().ReleaseMutex();
            if (toLocked) to.GetMutex().ReleaseMutex();
        }
    }

    static void Main()
    {
        BankAccount account1 = new BankAccount(100);
        BankAccount account2 = new BankAccount(100);

        Thread[] threads = new Thread[]
        {
            new Thread(() => account1.Deposit(50)),
            new Thread(() => account1.Withdraw(30)),
            new Thread(() => account2.Deposit(20)),
            new Thread(() => account2.Withdraw(40)),
            new Thread(() => Transfer(account1, account2, 50)),  // Transfer money
            new Thread(() => Transfer(account2, account1, 20)),   // Transfer in opposite direction
            new Thread(() => account1.SetBalance(200)),  // Manually update balance
            new Thread(() => account2.SetBalance(300))   // Manually update balance
        };

        foreach (Thread t in threads) t.Start();
        foreach (Thread t in threads) t.Join();  // Ensure all threads complete before printing final balances

        Console.WriteLine($"Final Balances - Account 1: {account1.GetBalance()}, Account 2: {account2.GetBalance()}");
    }
}
