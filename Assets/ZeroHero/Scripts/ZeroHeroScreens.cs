using UnityEngine;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame
    {
        void DrawHUD()
        {
            Fill(new Rect(0,0,1600,117),C("141613"));
            Text("체력",54,25,65,29,18,mutedColor);
            for(int i=0;i<20;i++)
            {
                var r=new Rect(120+i*13,31,10,19); Fill(r,C("35362C"));
                if(hp>i*5) Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01((hp-i*5)/5),r.height),hp<=25?ZeroHeroArt.Pink:C("B57960"));
            }
            Text(Mathf.CeilToInt(hp)+"/100",398,21,170,37,24,textColor,bold:true);
            Text((Act+1)+"구역  ·  "+(ZeroContent.IsBossRoom(room)?"보스전":((room-1)%4+1)+" / 3"),613,22,374,40,27,textColor,TextAnchor.MiddleCenter,true);
            Text(room+" / 20 맵",650,73,300,28,16,mutedColor,TextAnchor.MiddleCenter);
            StatIcon(3,1120,27,ZeroHeroArt.Gold); Text(wallet+" G",1164,21,175,40,27,ZeroHeroArt.Gold);
            if(Button(phase==Phase.Combat?"일시정지":"조작법",new Rect(1352,22,194,45))) { if(phase==Phase.Combat) paused=true; else help=true; }
            Text((Weapon.gun?"[1] ":"[2] ")+Weapon.name+"   피해 "+Signed(Weapon.Damage(stats)),54,75,560,30,18,Weapon.Damage(stats)<0?ZeroHeroArt.Gain:ZeroHeroArt.Gold);
            Text(TimeText(runTime)+"  ·  "+kills+" 처치"+(shields>0?"  ·  보호막 "+shields:""),1048,77,498,25,15,mutedColor,TextAnchor.UpperRight);
            DrawStatBar();
        }
        void DrawStatBar()
        {
            Fill(new Rect(0,791,1600,109),C("141613")); Fill(new Rect(54,792,1492,2),lineColor);
            for(int i=0;i<4;i++)
            {
                float x=54+i*310; Color ink=stats[i]<0?ZeroHeroArt.Pink:textColor;
                StatIcon(i,x,819,ink); Text(ZeroStats.Name(i),x+41,811,167,32,18,mutedColor);
                Text(Signed(stats[i]),x+191,806,85,46,30,ink,TextAnchor.UpperRight,true);
                Text(StatDetail(i,stats[i]),x+41,855,253,30,14,stats[i]<=0?ZeroHeroArt.Pink:mutedColor);
            }
            Text("총합 "+stats.Total,1336,812,210,35,22,textColor,TextAnchor.UpperRight);
            Text("변화량 "+(stats.Total-ZeroStats.InitialTotal),1336,855,210,26,15,mutedColor,TextAnchor.UpperRight);
        }
        string StatDetail(int index,int value)
        {
            if(index==0) return value<0?"일반 적 회복 / 언데드 2배 피해":"무기 피해 × "+(value/3f).ToString("0.00");
            if(index==1) return value<0?"받는 피해 +"+-value*2:"받는 피해 −"+value*2;
            if(index==2) return value==0?"수평 이동 불가 · 회피 가능":(value<0?"반대로 ":"이동 ")+(Mathf.Abs(value)*1.6f).ToString("0.0")+" m/s";
            return value<0?"동전 가치 × "+-value+" 손실":"동전 가치 × "+value;
        }
        void DrawCombat()
        {
            Text(currentMap.name,55,136,500,33,22,textColor,bold:true);
            Text("남은 적 "+(enemies.Count+spawnQueue.Count),57,177,360,28,16,mutedColor);
            Text(autoAim?"자동 조준 [Q]":"마우스 조준 [Q]",1190,138,356,27,16,mutedColor,TextAnchor.UpperRight);
            foreach(var e in enemies)
            {
                if(!e.Def.Boss) continue;
                Text(e.Def.name+(e.hp<e.maxHp*.5f?" · 격노":""),546,137,508,34,23,ZeroHeroArt.Gold,TextAnchor.MiddleCenter,true);
                Fill(new Rect(546,182,508,8),C("44352A")); Fill(new Rect(546,182,508*e.hp/e.maxHp,8),ZeroHeroArt.Pink);
                Text(bossNotice,397,211,806,31,17,e.casting?ZeroHeroArt.Gold:mutedColor,TextAnchor.MiddleCenter);
            }
            foreach(var f in floating)
            {
                Vector3 s=cam.WorldToScreenPoint(f.pos); var c=f.color; c.a=Mathf.Min(1,f.life*2);
                Text(f.text,(s.x-guiOffsetX)/guiScale-80,(Screen.height-s.y-guiOffsetY)/guiScale-25,160,38,20,c,TextAnchor.MiddleCenter,true);
            }
            if(petrified>0) Text("석화 "+petrified.ToString("0.0")+"초",590,286,420,55,33,C("CDD1C7"),TextAnchor.MiddleCenter,true);
            if(noticeTime>0) Text(notice,303,744,994,34,17,ZeroHeroArt.Gold,TextAnchor.MiddleCenter);
            Ability("Shift","회피",dashTimer,1.35f,57,723); Ability("E","폭탄",bombTimer,6,1344,723);
        }
        void Ability(string key,string name,float cooldown,float max,float x,float y)
        {
            Fill(new Rect(x-7,y-4,211,55),new Color(.075f,.08f,.065f,.94f));
            Text(key,x,y+2,69,30,18,textColor,bold:true); Text(cooldown>0?cooldown.ToString("0.0")+"초":name,x+72,y+2,125,30,18,cooldown>0?mutedColor:textColor);
            Fill(new Rect(x,y+40,193,3),C("363A2E")); Fill(new Rect(x,y+40,193*(1-Mathf.Clamp01(cooldown/max)),3),ZeroHeroArt.Gold);
        }
        void OpenSheet(string title,string description)
        {
            Fill(new Rect(0,116,1600,675),new Color(.065f,.07f,.055f,.975f));
            Text(title,260,161,1080,65,42,textColor,bold:true); Text(description,263,243,1074,45,19,mutedColor);
        }

        void DrawCamp()
        {
            Fill(new Rect(0,116,1600,675),C("151811"));
            Text("상점",100,142,430,60,37,textColor,bold:true);
            Text(completedRooms==0?"출발 준비 · 기본 권총과 칼을 보유하고 있습니다.":"맵 클리어 회복 +12 · 능력치 교환 적용됨",495,158,950,34,18,mutedColor);
            if(Button("총 10종",new Rect(100,213,230,43),shopTab==0)) shopTab=0;
            if(Button("칼 10종",new Rect(345,213,230,43),shopTab==1)) shopTab=1;
            if(Button("소모품",new Rect(590,213,230,43),shopTab==2)) shopTab=2;
            if(shopTab<2)
            {
                Text("무기",155,276,270,30,16,mutedColor); Text("기본 / 현재 피해",480,276,220,30,16,mutedColor);
                Text("초당 공격",745,276,180,30,16,mutedColor); Text(shopTab==0?"탄속 m/s":"사거리 m",928,276,150,30,16,mutedColor);
                Text("특징",1085,276,240,30,16,mutedColor);
                for(int row=0;row<10;row++)
                {
                    var w=ZeroContent.Weapons[shopTab*10+row]; float y=309+row*37;
                    if(activeWeapon==w.id) Fill(new Rect(100,y-1,1400,35),C("2B3020"));
                    Text(w.name,154,y+2,300,31,18,textColor, bold:activeWeapon==w.id);
                    Text(w.power+" / "+Signed(w.Damage(stats)),480,y+2,215,30,18,w.Damage(stats)<0?ZeroHeroArt.Gain:textColor);
                    Text(w.rate.ToString("0.00"),755,y+2,150,30,18,textColor);
                    Text((w.gun?w.bulletSpeed:w.reach).ToString("0.0"),951,y+2,126,30,18,textColor);
                    Text(w.gun?(w.pellets>1?w.pellets+"발 산탄":w.pierce>1?w.pierce+"개체 관통":"단발 탄환"):(w.rate>=3?"빠른 검":w.reach>=3?"긴 검":"한손 검"),1085,y+3,209,29,16,mutedColor);
                    bool has=owned.Contains(w.id); string caption=activeWeapon==w.id?"장착 중":has?"장착":w.price+" G";
                    if(Button(caption,new Rect(1300,y,200,31),false,activeWeapon!=w.id&&(has||wallet>=w.price))) BuyWeapon(w.id);
                }
            }
            else
            {
                Text("물약",150,323,500,45,30,textColor,bold:true); Text("체력 40 회복 · 맵마다 1회 구매",150,380,780,35,20,mutedColor);
                string h=campHealed?"구매 완료":hp>=100?"체력 최대":wallet<24?"돈 부족":"24 G 구매";
                if(Button(h,new Rect(1160,340,340,57),false,!campHealed&&hp<100&&wallet>=24)) BuySupply(false);
                Fill(new Rect(100,448,1400,1),lineColor);
                Text("보호막",150,486,500,45,30,textColor,bold:true); Text("다음 공격 1회 막기 · 맵마다 1회 구매",150,543,780,35,20,mutedColor);
                string s=campShield?"구매 완료":wallet<18?"돈 부족":"18 G 구매";
                if(Button(s,new Rect(1160,502,340,57),false,!campShield&&wallet>=18)) BuySupply(true);
            }
            Text("1 총 / 2 칼 · Tab 전환    |    무기 구매는 능력치 총합을 바꾸지 않습니다.",102,723,1000,36,16,mutedColor);
            if(Button("맵 선택 [Enter]",new Rect(1140,702,360,56),true)) NextRoom();
        }

        void DrawRoute()
        {
            Fill(new Rect(0,116,1600,675),C("151811"));
            Text(ZeroContent.IsBossRoom(room)?"보스전으로":"다음 맵",100,143,1150,61,38,textColor,bold:true);
            Text(ZeroContent.IsBossRoom(room)?"보스는 같습니다. 발판 배치를 선택하세요.":"적 종류와 특성을 확인하고 하나를 고르세요.",103,213,1330,36,19,mutedColor);
            for(int i=0;i<3;i++)
            {
                var m=routes[i]; float x=100+i*475; var rect=new Rect(x,271,450,405); bool active=routeSelected==i;
                Fill(rect,active?C("303124"):C("22271E")); Border(rect,active?ZeroHeroArt.Gold:lineColor,active?3:1);
                Text((i+1)+"   "+m.name,x+22,290,407,43,26,textColor,bold:true);
                DrawMapPreview(m,new Rect(x+22,344,406,81));
                Text(m.terrain,x+22,437,407,31,17,ZeroHeroArt.Gold);
                Text(m.Roster,x+22,482,407,49,20,textColor,bold:true);
                float y=537;
                if(m.Boss)
                {
                    Text(m.detail,x+22,y,407,76,17,mutedColor);
                    string adds=m.counts[(int)EnemyKind.Heart]>0?"소환: 언데드 3":m.counts[(int)EnemyKind.DemonKing]>0?"소환: 오크 2 · 고블린 2":"공격 전에 예고 표시";
                    Text(adds,x+22,634,407,26,15,ZeroHeroArt.Pink);
                }
                else for(int k=0;k<6;k++) if(m.counts[k]>0) { Text(ZeroContent.Enemies[k].detail,x+22,y,407,53,17,k==(int)EnemyKind.Undead?ZeroHeroArt.Gain:mutedColor); y+=55; }
                if(GUI.Button(rect,GUIContent.none,GUIStyle.none)) { routeSelected=i; PlaySound(6); }
            }
            Text("1 / 2 / 3 선택 · Enter 확정",103,724,780,31,18,mutedColor);
            if(Button("이 맵으로 [Enter]",new Rect(1140,702,360,56),true,routeSelected>=0)) ChooseRoute();
        }
        void DrawMapPreview(MapOffer map,Rect r)
        {
            Color rock=C(StoneColors[(int)map.theme]), sky=C(SkyColors[(int)map.theme]); Fill(r,sky);
            Color distant=Color.Lerp(rock,sky,.65f);
            for(int i=0;i<6;i++)
            {
                float x=r.x+10+i*65;
                if(map.theme==MapTheme.GiantPass || map.theme==MapTheme.OrcCamp)
                {
                    for(int step=0;step<6;step++) Fill(new Rect(x+step*4,r.yMax-16-step*7,54-step*8,8),distant);
                }
                else if(map.theme==MapTheme.Graveyard)
                {
                    Fill(new Rect(x+15,r.yMax-32,21,22),distant);
                    Fill(new Rect(x+24,r.yMax-28,3,12),rock);
                    Fill(new Rect(x+20,r.yMax-25,11,3),rock);
                }
                else if(map.theme==MapTheme.ElfForest || map.theme==MapTheme.Swamp)
                {
                    Fill(new Rect(x+24,r.y+8,6,61),distant);
                    Fill(new Rect(x+9,r.y+26,36,5),distant);
                    if(map.theme==MapTheme.ElfForest) Fill(new Rect(x+1,r.y+10,50,18),distant);
                }
                else
                {
                    Fill(new Rect(x+15,r.y+12,14,57),distant);
                    Fill(new Rect(x+9,r.y+9,map.theme==MapTheme.GoblinMine?65:26,5),distant);
                }
            }
            Fill(new Rect(r.x,r.yMax-10,r.width,10),rock);
            foreach(var ledge in Ledges[map.layout])
                Fill(new Rect(r.center.x+(ledge.x-ledge.z*.5f)*r.width/32,r.yMax-10-(ledge.y-Floor)*9,ledge.z*r.width/32,4),rock*1.2f);
        }

        void DrawEnd()
        {
            bool won=phase==Phase.Victory;
            OpenSheet(won?"마왕 처치":"사망",won?"20개 맵과 다섯 보스를 모두 클리어했습니다.":currentMap.name+"에서 쓰러졌습니다.");
            string[] labels={"클리어","처치","시간"}; string[] values={completedRooms+" / 20 맵",kills+"마리",TimeText(runTime)};
            for(int i=0;i<3;i++) { float x=278+i*364; Text(labels[i],x,367,310,33,18,mutedColor); Text(values[i],x,417,310,66,38,textColor,bold:true); }
            Fill(new Rect(263,525,1077,1),lineColor);
            if(Button("다시 시작 [Enter]",new Rect(263,581,515,63),true)) StartRun();
            if(Button("메인 메뉴",new Rect(805,581,535,63))) ReturnToTitle();
            Text("최고 기록 "+best+" / 20 맵",264,704,800,30,18,mutedColor);
        }
        void DrawHelp()
        {
            Dim(.985f); Text("조작법",130,75,1000,70,42,textColor,bold:true);
            string[] keys={"A / D, ← / →","Space / W / ↑","S + Space","클릭 / J","1 / 2 / Tab","Shift / E","Q / Esc / M"};
            string[] actions={"좌우 이동","2단 점프","발판 아래로 내려가기","장착한 무기로 공격","총 / 칼 / 무기 전환","회피 / 폭탄 (6초 재충전)","자동 조준 / 일시정지 / 소리"};
            for(int i=0;i<keys.Length;i++) { Text(keys[i],130,199+i*57,260,39,21,ZeroHeroArt.Gold); Text(actions[i],403,199+i*57,445,39,21,textColor); }
            Text("적과 능력치",892,199,550,44,27,textColor,bold:true);
            Text("음수 공격: 일반 적은 회복, 언데드는 2배 피해.\n음수 방어: 받는 피해 증가.\n음수 이동: 좌우가 반전. 공격 방향은 유지.\n음수 자금: 동전 획득 시 보유금 차감.",892,267,570,170,19,ZeroHeroArt.Pink);
            Text("무기 피해 = 기본 피해 × 공격력 / 3\n총의 표기 피해는 탄환 한 발 기준입니다.\n칼은 사거리 안의 여러 적을 함께 벱니다.\n공격력이 0 이하라도 폭탄은 피해 42입니다.",892,468,570,159,18,mutedColor);
            Text("일반 맵 3개 → 보스전. 총 5구역입니다.\n맵 클리어 → 능력치 교환 → 상점 → 맵 선택.\n남은 동전은 자동 수집되고 체력 12를 회복합니다.",130,646,700,113,18,mutedColor);
            Text("능력치 총합 10, 교환의 변화량 합계 0.\n무기와 보유금은 사망하면 초기화됩니다.\n메두사의 석화는 1.35초 동안 행동을 막습니다.",892,658,570,95,18,mutedColor);
            if(Button("닫기 [Esc]",new Rect(1120,792,340,57),true)) { help=false; paused=false; }
        }
    }
}
