using UnityEngine;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame
    {
        static readonly string[] SkyColors = { "252C2B","202329","2E2D23","211D1A","1A302C","2B3339","262E28","454039","2A131C","22291D","241D27" };
        static readonly string[] StoneColors = { "656A53","616563","716043","6B5441","597849","7B817A","798371","C2B68A","91515A","596343","655363" };
        // Shared by the playable terrain and the route-card preview: x, top, width.
        static readonly Vector3[][] Ledges = {
            new[] { new Vector3(-8,-2.5f,4.5f), new Vector3(0,-.2f,4), new Vector3(8,-2.5f,4.5f) },
            new[] { new Vector3(-7,-.8f,4.6f), new Vector3(0,-3,3.6f), new Vector3(7,-.8f,4.6f) },
            new[] { new Vector3(-8,-2.8f,4), new Vector3(0,-1.8f,4.2f), new Vector3(8,-2.8f,4) }
        };
        void BuildMap()
        {
            if (scenery != null) Destroy(scenery.gameObject);
            scenery = new GameObject(currentMap.name).transform; platforms.Clear();
            int theme = (int)currentMap.theme; Color sky = C(SkyColors[theme]), stone = C(StoneColors[theme]);
            Back("Sky",art.square,sky,Vector2.zero,new Vector2(34,18),-60);
            for (int i = 0; i < 7; i++) Back("Haze",art.square,Color.Lerp(sky,C("101511"),i*.08f),new Vector2(0,-3+i*1.5f),new Vector2(34,1.6f),-59);
            switch (currentMap.theme)
            {
                case MapTheme.Outpost:
                case MapTheme.Temple:
                case MapTheme.Citadel:
                    for (int i = -3; i <= 3; i++)
                    {
                        float x = i*4.7f; Color rock = Color.Lerp(stone,sky,.64f);
                        Back("Tower",art.square,rock,new Vector2(x,-.5f),new Vector2(2.3f,9),-50);
                        Back("Window",art.square,currentMap.theme == MapTheme.Citadel ? C("843C36") : sky*.55f,new Vector2(x,1.2f),new Vector2(.65f,2.1f),-49);
                        for (int j = 0; j < 5; j++) Back("Masonry",art.square,sky*.8f,new Vector2(x,-4+j*1.8f),new Vector2(2.3f,.035f),-48);
                        Back("Capital",art.square,rock*1.2f,new Vector2(x,3.9f),new Vector2(2.8f,.4f),-48);
                    }
                    if (currentMap.theme == MapTheme.Temple)
                        for (int i = -1; i <= 1; i++) Back("Statue",art.creatures[(int)EnemyKind.Human],C("626F62"),new Vector2(i*9,-2.7f),Vector2.one*2.5f,-42);
                    break;
                case MapTheme.Graveyard:
                    Back("Moon",art.disc,C("848B7D"),new Vector2(-9,3.5f),Vector2.one*2.1f,-55);
                    for (int i = 0; i < 14; i++)
                    {
                        float x = -14+i*2.1f, h = .6f+(i%3)*.24f;
                        Back("Grave",art.square,C("424D46"),new Vector2(x,Floor+h/2),new Vector2(.8f,h),-42);
                        Back("Cross",art.square,C("72786A"),new Vector2(x,Floor+h*.75f),new Vector2(.07f,.42f),-41);
                        Back("Cross",art.square,C("72786A"),new Vector2(x,Floor+h*.78f),new Vector2(.32f,.06f),-41);
                    }
                    Trees(stone*.6f,false); break;
                case MapTheme.ElfForest:
                    Trees(C("355641"),true);
                    for (int i = 0; i < 9; i++) Back("Far canopy",art.disc,C("284637"),new Vector2(-15+i*4,4+i%2),new Vector2(7,4),-52);
                    break;
                case MapTheme.Swamp:
                    Trees(C("414A2E"),false);
                    for (int i = 0; i < 8; i++) Back("Fog",art.square,new Color(.35f,.42f,.21f,.1f),new Vector2(0,-4+i*.6f),new Vector2(32,.2f),-40);
                    Back("Swamp water",art.square,C("3B4C30"),new Vector2(0,-5.6f),new Vector2(32,1.2f),-30); break;
                case MapTheme.OrcCamp:
                    for (int i = -2; i <= 2; i++)
                    {
                        float x=i*6;
                        for (int r=0;r<9;r++) Back("Tent",art.square,C(i%2==0?"625537":"534731"),new Vector2(x,-3+r*.22f),new Vector2(4-r*.35f,.25f),-49);
                        Back("Entrance",art.square,C("24271D"),new Vector2(x,-3.2f),new Vector2(.8f,1.6f),-48);
                        Back("Banner pole",art.square,C("655440"),new Vector2(x+2,-1.2f),new Vector2(.12f,6),-47);
                        Back("Banner",art.square,C("8E4936"),new Vector2(x+2.5f,.9f),new Vector2(.85f,1.5f),-46);
                    }
                    break;
                case MapTheme.GoblinMine:
                    for (int i=-3;i<=3;i++)
                    {
                        Back("Mine support",art.square,C("514333"),new Vector2(i*4.5f,-.5f),new Vector2(.4f,10),-49);
                        Back("Mine beam",art.square,C("514333"),new Vector2(i*4.5f,3.3f),new Vector2(4.7f,.4f),-48);
                        for(int r=0;r<4;r++) Back("Rock teeth",art.square,C("39362B"),new Vector2(i*4.5f+1.7f,4.5f-r*.25f),new Vector2(.8f-r*.18f,.3f),-44);
                        Back("Ore",art.square,C("8C9F71"),new Vector2(i*4.5f+1,-3),new Vector2(.25f,.6f),-44).transform.rotation=Quaternion.Euler(0,0,25);
                    }
                    break;
                case MapTheme.GiantPass:
                    for(int i=-2;i<=2;i++) for(int r=0;r<18;r++)
                        Back("Mountain",art.square, i%2==0 ? C("4C5855") : C("394542"),new Vector2(i*8,-4+r*.5f),new Vector2(10-r*.48f,.52f),-53+i);
                    for(int i=-2;i<=2;i++) Back("Boulder",art.disc,C("586354"),new Vector2(i*7,Floor+.3f),new Vector2(2.8f,1.5f),-40);
                    break;
                case MapTheme.Heaven:
                    for(int i=0;i<12;i++)
                    {
                        Back("Cloud",art.disc,C("6E6B59"),new Vector2(-15+i*3,3+(i%3)*.45f),new Vector2(5.5f,1.9f),-53);
                        if(i%2==0) { Back("Ivory column",art.square,C("9C987C"),new Vector2(-14+i*2.8f,-.7f),new Vector2(.75f,8),-48); Back("Column head",art.square,C("B3AB85"),new Vector2(-14+i*2.8f,3.5f),new Vector2(1.35f,.4f),-47); }
                    }
                    break;
                case MapTheme.Viscera:
                    for(int i=0;i<16;i++)
                    {
                        float x=-15+i*2;
                        Back("Tissue",art.disc,C(i%2==0?"592D38":"492431"),new Vector2(x,1),new Vector2(3,11),-49);
                        var vein=Back("Artery",art.square,C("783D48"),new Vector2(x,.3f),new Vector2(.18f,10),-48); vein.transform.rotation=Quaternion.Euler(0,0,(i%3-1)*8);
                    }
                    break;
            }
            Back("Soil",art.square,Color.Lerp(stone,C("1B2019"),.56f),new Vector2(0,-6.3f),new Vector2(32,3),-20);
            Back("Ground ledge",art.square,stone,new Vector2(0,Floor-.16f),new Vector2(32,.32f),-19);
            for(int x=-15;x<=15;x++)
            {
                Back("Ground chips",art.square,stone*.78f,new Vector2(x,Floor-.36f),new Vector2(.87f,.24f),-18);
                for(int y=0;y<2;y++) Back("Embedded stones",art.square,stone*.62f,new Vector2(x+(y%2)*.3f,-5.7f-y*.65f),new Vector2(.7f,.37f),-18);
            }
            foreach(var ledge in Ledges[currentMap.layout]) Platform(ledge.x,ledge.y,ledge.z,stone);
        }
        SpriteRenderer Back(string name,Sprite sprite,Color color,Vector2 pos,Vector2 scale,int order)
            => Sprite(name,sprite,color,pos,scale,order,scenery);
        void Platform(float x,float top,float width,Color color)
        {
            platforms.Add(new Rect(x-width/2,top-.3f,width,.3f));
            Back("Platform",art.square,color,new Vector2(x,top-.14f),new Vector2(width,.28f),-5);
            Back("Platform edge",art.square,color*1.2f,new Vector2(x,top-.025f),new Vector2(width,.05f),-4);
            for(int i=0;i<4;i++) Back("Ledge stones",art.square,color*.72f,new Vector2(x-width/2+.35f+i*(width-.7f)/3,top-.35f),new Vector2(.55f,.2f),-6);
        }
        void Trees(Color color,bool leaves)
        {
            for(int i=0;i<7;i++)
            {
                float x=-15+i*5;
                Back("Trunk",art.square,color,new Vector2(x,0),new Vector2(.75f,10),-49);
                for(int j=0;j<3;j++)
                {
                    var branch=Back("Branch",art.square,color,new Vector2(x+(j%2==0?1:-1),j*.9f+.5f),new Vector2(3.4f,.3f),-48); branch.transform.rotation=Quaternion.Euler(0,0,j%2==0?25:-25);
                    if(leaves) Back("Canopy",art.disc,color*1.3f,new Vector2(x,3.4f+j*.5f),new Vector2(5,2),-47);
                }
            }
        }
    }
}
