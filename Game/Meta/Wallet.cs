using System;

namespace SwampFrog;

/// <summary>
/// Кошелёк мета-прогрессии: баланс валюты «Слизь». Чистый C# класс без привязки
/// к Godot-нодам, чтобы начисление и трату было легко тестировать (см. docs/META.md).
/// </summary>
public sealed class Wallet
{
	private int _balance;

	/// <summary>Баланс слизи.</summary>
	public int Balance => _balance;

	/// <summary>Баланс изменился.</summary>
	public event Action? Changed;

	/// <summary>Начисляет слизь; ноль и отрицательные значения игнорируются.</summary>
	public void Add(int amount)
	{
		if (amount <= 0)
			return;
		_balance += amount;
		Changed?.Invoke();
	}

	/// <summary>Списывает слизь. Возвращает false, если не хватает или сумма неположительна.</summary>
	public bool Spend(int amount)
	{
		if (amount <= 0 || amount > _balance)
			return false;
		_balance -= amount;
		Changed?.Invoke();
		return true;
	}

	/// <summary>Задаёт баланс при загрузке сохранения; отрицательное значение становится нулём.</summary>
	public void SetBalance(int value) => _balance = Math.Max(0, value);
}
