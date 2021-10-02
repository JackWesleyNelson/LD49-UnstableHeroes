using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit
{
    public string name;
    public int level, damage, maxHP, currentHP, currentThreatLevel = 5, maxThreatLevel = 9, MinThreatLevel = 1;
    public bool turnTaken;

    public Unit(string name, int level, int damage, int maxHP)
    {
        this.name = name;
        this.level = level;
        this.damage = damage;
        this.maxHP = maxHP;
        this.currentHP = maxHP;
        this.turnTaken = false;
    }

    public Unit(Unit unit)
    {
        this.name = unit.name;
        this.level = unit.level;
        this.damage = unit.damage;
        this.maxHP = unit.maxHP;
        this.currentHP = unit.maxHP;
        this.turnTaken = false;
    }

    public bool TakeDamage(int damage)
    {
        this.currentHP -= damage;
        if(currentHP < 0)
        {
            return true;
        }
        return false;
    }

    public bool IsDead()
    {
        return TakeDamage(0);
    }

    public void Heal(int health)
    {
        currentHP += health;
        if(currentHP > maxHP) currentHP = maxHP;
    }

    public void Intimidate()
    {
        ModifyThreat(2);
    }

    public void Hide()
    {
        ModifyThreat(-2);
    }

    public void Rest()
    {
        Heal(1);
    }

    public void ModifyThreat(int toAdd)
    {
        currentThreatLevel -= toAdd;
        currentThreatLevel = Mathf.Clamp(currentThreatLevel, MinThreatLevel, maxThreatLevel);
    }

}
