using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit
{
    public string name;
    public int level, damage, restoration, maxHP, currentHP, currentThreatLevel = 5, maxThreatLevel = 9, MinThreatLevel = 0, defensiveStance;
    public bool turnTaken;
    public List<(Action, String)> stableActionsAndNames = new List<(Action, string)>();

    public Unit(string name, int level, int damage, int restoration, int maxHP)
    {
        this.name = name;
        this.level = level;
        this.damage = damage;
        this.restoration = restoration;
        this.maxHP = maxHP;
        this.currentHP = maxHP;
        this.turnTaken = false;
        this.defensiveStance = 0;
    }

    public Unit(Unit unit)
    {
        this.name = unit.name;
        this.level = unit.level;
        this.damage = unit.damage;
        this.restoration = unit.restoration;
        this.maxHP = unit.maxHP;
        this.currentHP = unit.maxHP;
        this.currentThreatLevel = unit.currentThreatLevel;
        this.turnTaken = false;
        this.defensiveStance = unit.defensiveStance;
        foreach((Action,String) pair in unit.stableActionsAndNames)
        {
            this.stableActionsAndNames.Add(pair);
        }
    }

    public bool TakeDamage(int damage)
    {
        if (defensiveStance > 0)
        {
            damage = Mathf.Clamp( damage / 2, 1, damage);
        }
        this.currentHP -= damage;
        if(currentHP <= 0)
        {
            return true;
        }
        return false;
    }

    public bool IsDead()
    {
        if(currentHP <= 0)
        {
            return true;
        }
        return false;
    }

    public void Heal(int health)
    {
        if(health < 0)
        {
            health++;
        }
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
        currentThreatLevel += toAdd;
        currentThreatLevel = Mathf.Clamp(currentThreatLevel, MinThreatLevel, maxThreatLevel);
    }

}
