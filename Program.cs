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
    static void TransferWithDeadlock(BankAccount from, BankAccount to, int amount) //Transfer that causes Deadlock
    {
        Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Attempting to transfer {amount}...");

        from.GetMutex().WaitOne();  // Lock the first account
        Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Locked first account");

        Thread.Sleep(500);  // Simulate delay to increase deadlock chance

        to.GetMutex().WaitOne();  // Lock the second account
        Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Locked second account");

        try
        {
            if (from.GetBalance() >= amount)
            {
                from.SetBalance(from.GetBalance() - amount);
                to.SetBalance(to.GetBalance() + amount);
                Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Transferred {amount}. New balances - From: {from.GetBalance()}, To: {to.GetBalance()}");
            }
            else
            {
                Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Transfer failed due to insufficient funds. From balance: {from.GetBalance()}");
            }
        }
        finally
        {
            from.GetMutex().ReleaseMutex();
            to.GetMutex().ReleaseMutex();
            Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Released both locks");
        }
    }

    static void TransferSafe(BankAccount from, BankAccount to, int amount) //Transfer w/o deadlock
{
    BankAccount first = from.GetHashCode() < to.GetHashCode() ? from : to;
    BankAccount second = from.GetHashCode() < to.GetHashCode() ? to : from;

    first.GetMutex().WaitOne();
    Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Locked first account");

    Thread.Sleep(100);  // Simulate delay

    second.GetMutex().WaitOne();
    Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Locked second account");

    try
    {
        if (from.GetBalance() >= amount)
        {
            from.SetBalance(from.GetBalance() - amount);
            to.SetBalance(to.GetBalance() + amount);
            Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Transferred {amount}. New balances - From: {from.GetBalance()}, To: {to.GetBalance()}");
        }
        else
        {
            Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Transfer failed due to insufficient funds. From balance: {from.GetBalance()}");
        }
    }
    finally
    {
        first.GetMutex().ReleaseMutex();
        second.GetMutex().ReleaseMutex();
        Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Released both locks");
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
            new Thread(() => TransferSafe(account1, account2, 50)),  // Transfer money
            new Thread(() => TransferSafe(account2, account1, 20)),   // Transfer in opposite direction
            new Thread(() => account1.SetBalance(200)),  // Manually update balance
            new Thread(() => account2.SetBalance(300)),   // Manually update balance
            new Thread(() => account1.Withdraw(500)),
            new Thread(() => account2.Withdraw(500))    // Should cause insufficient funds
        };

        foreach (Thread t in threads) t.Start();
        foreach (Thread t in threads) t.Join();  // Ensure all threads complete before printing final balances

        Console.WriteLine($"Final Balances - Account 1: {account1.GetBalance()}, Account 2: {account2.GetBalance()}");
    }
}
