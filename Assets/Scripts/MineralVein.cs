using UnityEngine;
using System.Collections.Generic;

public class MineralVein : MonoBehaviour
{
    private Materials initialOreType;
    public int RemainingYield { get; private set; }

    public bool isRandomSpecial { get; private set; } = false;
    private List<Materials> randomOreList;
    private float randomTickInterval;
    private Vector2Int randomAmountRange;

    private Materials lastPickedOre;

    public Materials oreType
    {
        get { return lastPickedOre; }
    }

    public int currentYield
    {
        get { return RemainingYield; }
    }
    //Initialize the mineral vein with a specific ore type and yield
    public void Initialize(Materials oreType, int yield)
    {
        this.initialOreType = oreType;
        this.lastPickedOre = oreType;
        this.RemainingYield = yield;

        var settings = MineralSpawner.Instance.GetOreData(oreType);
        if (settings != null)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = settings.oreColor;
        }
    }
    //initialize the mineral veins with the ranm special settings
    public void InitializeRandom(List<Materials> ores, float tickInterval, Vector2Int amountRange)
    {
        isRandomSpecial = true;
        randomOreList = new List<Materials>(ores);
        randomTickInterval = tickInterval;
        randomAmountRange = amountRange;
        RemainingYield = int.MaxValue;

        RemainingYield = int.MaxValue;

        lastPickedOre = randomOreList.Count > 0
                            ? randomOreList[0]
                            : throw new System.InvalidOperationException("Random list empty");

        var rend = GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = Color.magenta;
    }
    //mining function, will mine either the normal ore or the random special ore
    public int Mine(int reqAmount)
    {
        if (isRandomSpecial)
        {
            lastPickedOre = randomOreList[Random.Range(0, randomOreList.Count)];
            int amt = Random.Range(randomAmountRange.x, randomAmountRange.y + 1);
            RemainingYield -= amt;
            MineralSpawner.Instance.UpdateVeinYield(gameObject, amt);
            return amt;
        }
        else
        {
            lastPickedOre = initialOreType;
            RemainingYield--;
            MineralSpawner.Instance.UpdateVeinYield(gameObject, 1);
            return 1;
        }
    }
}
