using UnityEngine;

public class ScrapInstance : Instance
{
    private AbstractScrap scrapData;

    public AbstractItem itemData => scrapData;

    public ScrapInstance(AbstractScrap scrapData)
    {
        this.scrapData = scrapData;
    }

    public void Use()
    {
        return;
    }
}