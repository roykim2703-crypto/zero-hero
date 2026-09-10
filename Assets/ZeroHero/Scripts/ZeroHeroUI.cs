using UnityEngine;

namespace ZeroHero
{
    public sealed partial class ZeroHeroGame
    {
        Font uiFont;
        GUIStyle labelStyle;
        readonly Color textColor = C("E8DFCB"), mutedColor = C("A99E8B"), panelColor = C("24231F"), lineColor = C("534D40");
        float guiScale, guiOffsetX, guiOffsetY;

        void InitUI()
        {
            if (uiFont != null) return;
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 24);
            labelStyle = new GUIStyle { font = uiFont, richText = false, wordWrap = true, alignment = TextAnchor.UpperLeft };
        }

        void OnGUI()
        {
            if (art == null) return;
            InitUI();
            guiScale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            guiOffsetX = (Screen.width - 1600 * guiScale) * .5f; guiOffsetY = (Screen.height - 900 * guiScale) * .5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(guiOffsetX, guiOffsetY, 0), Quaternion.identity, Vector3.one * guiScale);
            GUI.enabled = !paused && !help;
            if (phase == Phase.Title) DrawTitle();
            else
            {
                DrawHUD();
                bool pageEnabled=GUI.enabled; if(shopOpen) GUI.enabled=false;
                if (phase == Phase.Combat || phase == Phase.RoomClear) DrawCombat();
                if (phase == Phase.RoomClear) DrawRoomClear();
                if (phase == Phase.Trade) DrawTrade();
                if (phase == Phase.Camp) DrawCamp();
                if (phase == Phase.Route) DrawRoute();
                if (phase == Phase.Dead || phase == Phase.Victory) DrawEnd();
                GUI.enabled=pageEnabled;
                if (!shopOpen && (phase == Phase.Route || phase == Phase.Trade)) DrawShopShortcut();
                if (shopOpen) DrawCamp();
            }
            GUI.enabled = true;
            if (paused && !help) DrawPause();
            if (help) DrawHelp();
            GUI.matrix = Matrix4x4.identity; GUI.color = Color.white;
        }

        void Fill(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white; }
        void Border(Rect rect, Color color, float thickness = 2)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color); Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color); Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
        Color FontColor(Color c) => QualitySettings.activeColorSpace == ColorSpace.Linear ? c.gamma : c;
        void Text(string value, float x, float y, float width, float height, int size = 20, Color? color = null, TextAnchor align = TextAnchor.UpperLeft, bool bold = false)
        {
            labelStyle.fontSize = size; labelStyle.normal.textColor = FontColor(color ?? textColor);
            labelStyle.alignment = align; labelStyle.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            GUI.Label(new Rect(x, y, width, height), value, labelStyle);
        }
        void Marker(float x, float y, Color color)
        { Fill(new Rect(x, y, 5, 19), color); Fill(new Rect(x + 5, y + 4, 5, 11), color); Fill(new Rect(x + 10, y + 8, 4, 3), color); }
        bool Button(string caption, Rect rect, bool primary = false, bool enabled = true)
        {
            bool hover = rect.Contains(Event.current.mousePosition) && enabled && GUI.enabled;
            Fill(new Rect(rect.x + 3, rect.y + 4, rect.width, rect.height), C("0D0E0D"));
            Fill(rect, hover ? C("3B3528") : panelColor);
            Border(rect, !enabled ? C("37362F") : primary || hover ? ZeroHeroArt.Gold : lineColor);
            Color ink = !enabled ? mutedColor : primary || hover ? ZeroHeroArt.Gold : textColor;
            if (hover) Marker(rect.x + 18, rect.center.y - 9, ink);
            bool old = GUI.enabled; GUI.enabled = old && enabled;
            bool result = GUI.Button(rect, GUIContent.none, GUIStyle.none); GUI.enabled = old;
            Text(caption, rect.x + 40, rect.y, rect.width - 80, rect.height, 21, ink, TextAnchor.MiddleCenter);
            return result;
        }
        void Dim(float alpha = .92f) => Fill(new Rect(0, 0, 1600, 900), new Color(.055f, .058f, .05f, alpha));
        void Image(Sprite sprite, Rect rect, Color color)
        { GUI.color = color; GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit); GUI.color = Color.white; }
        static string Signed(int value) => value > 0 ? "+" + value : value < 0 ? "−" + -value : "0";
        static string Signed(float value)
        {
            if (Mathf.Abs(value) < .001f) return "0";
            return (value > 0 ? "+" : "−") + Mathf.Abs(value).ToString("0.0");
        }
        static string TimeText(float time) => ((int)time / 60).ToString("00") + ":" + ((int)time % 60).ToString("00");

        void DrawTitle()
        {
            Fill(new Rect(0, 0, 1600, 900), C("161815"));
            DrawEntrance();
            Text("제로의", 128, 188, 670, 130, 104, textColor, bold: true);
            Text("용사", 122, 305, 590, 174, 142, textColor, bold: true);
            Fill(new Rect(136, 483, 57, 5), ZeroHeroArt.Pink);
            Text("능력치 하나를 올리면, 다른 하나가 내려갑니다.", 135, 522, 665, 40, 22, mutedColor);
            if (Button("시작", new Rect(136, 606, 310, 61), true)) StartRun();
            if (Button("조작법", new Rect(136, 687, 310, 57))) help = true;
            Text("Enter  시작", 468, 623, 230, 29, 16, mutedColor);
            float permanent=0; for(int i=0;i<ZeroStats.Count;i++) permanent+=metaLevels[i]*.1f;
            Text("영구 성장 +"+permanent.ToString("0.0")+" · 보유 잔재 "+legacyPoints,136,778,690,28,16,ZeroHeroArt.Gold);
            Text(best > 0 ? "최고 기록  " + best + " / 20 맵" : "일반 맵 3개 → 보스전 · 총 5구역", 136, 816, 690, 32, 17, mutedColor);
            Text("도전 "+totalRuns+" · 사망 "+totalDeaths+" · 승리 "+totalVictories+" · 처치 "+lifetimeKills,136,854,850,28,15,mutedColor);
            if (Button(muted ? "소리: 꺼짐" : "소리: 켜짐", new Rect(1295, 808, 190, 44))) ToggleSound();
        }

        void DrawEntrance()
        {
            // Masonry and steps share a fixed pixel grid with the character.
            Fill(new Rect(868, 177, 585, 570), C("1B1E19"));
            for (int row = 0; row < 9; row++) for (int col = 0; col < 6; col++)
            {
                float x = 843 + col * 104 + (row % 2) * 34, y = 202 + row * 60;
                if (x > 1460) continue;
                Fill(new Rect(x, y, 98, 54), (row + col) % 3 == 0 ? C("242820") : C("20241E"));
                Fill(new Rect(x + 7, y + 4, 74, 3), C("2A2E25"));
            }
            Fill(new Rect(1020, 249, 256, 397), C("090C0A"));
            Fill(new Rect(1052, 220, 192, 65), C("090C0A"));
            for (int i = 0; i < 6; i++)
            {
                float y = 279 + i * 58;
                Fill(new Rect(965 + i % 2 * 4, y, 55, 53), i % 2 == 0 ? C("4E5041") : C("424737"));
                Fill(new Rect(972, y + 4, 41, 5), C("646450"));
                Fill(new Rect(1276, y, 51 + i % 2 * 5, 53), C("353C30"));
                Fill(new Rect(1276, y + 5, 5, 44), C("4B5140"));
            }
            Fill(new Rect(993, 239, 60, 37), C("565743"));
            Fill(new Rect(1022, 208, 59, 37), C("60604B"));
            Fill(new Rect(1086, 196, 59, 42), C("5A5A45"));
            Fill(new Rect(1150, 196, 59, 42), C("4A4F3D"));
            Fill(new Rect(1214, 211, 59, 35), C("424A38"));
            Fill(new Rect(1248, 241, 59, 34), C("3B4434"));
            for (int i = 0; i < 5; i++)
            {
                float x = 1004 - i * 23, y = 641 + i * 20, width = 285 + i * 46;
                Fill(new Rect(x, y, width, 17), Color.Lerp(C("494938"), C("282D23"), i / 5f));
                Fill(new Rect(x + 6, y, width - 12, 3), C("5A5741"));
                Fill(new Rect(x + 62 + i * 33, y + 2, 4, 13), C("252A21"));
            }
            float flicker = Mathf.Floor(Mathf.Sin(visualTime * 9) * 2) * 4;
            Fill(new Rect(918, 401, 10, 62), C("3F3020"));
            Fill(new Rect(906, 398, 34, 8), C("716044"));
            Fill(new Rect(910, 365 + flicker, 25, 33 - flicker), C("B67535"));
            Fill(new Rect(917, 356 - flicker, 11, 38 + flicker), C("D6A854"));
            Fill(new Rect(919, 380, 8, 16), C("EDCE86"));
            Image(art.hero, new Rect(1096, 518, 104, 130), Color.white);
            for (int i = 0; i < 12; i++)
            {
                float x = 865 + i * 47, y = 740 + (i * 31 % 52);
                Fill(new Rect(x, y, 7 + i % 3 * 7, 5 + i % 2 * 4), C("393B2E"));
            }
        }

        void StatIcon(int index, float x, float y, Color ink, int scale = 3)
        {
            string[] rows;
            if (index == 0) rows = new[] { "......##", ".....##.", "....##..", "#..##...", ".###....", "..##....", ".#.##...", "#......." };
            else if (index == 1) rows = new[] { ".######.", ".##..##.", ".#....#.", ".#....#.", ".##..##.", "..#..#..", "...##...", "........" };
            else if (index == 2) rows = new[] { "..###...", "..###...", "..###...", "..###...", "..#####.", ".######.", ".######.", "........" };
            else if(index==4) rows = new[] { "...##...", "#..##..#", ".######.", "..####..", ".######.", "#..##..#", "...##...", "........" };
            else rows = new[] { "..####..", ".##..##.", "##.##.##", "##.#..##", "##..#.##", "##.##.##", ".##..##.", "..####.." };
            for (int r = 0; r < rows.Length; r++) for (int c = 0; c < rows[r].Length; c++)
                if (rows[r][c] == '#') Fill(new Rect(x + c * scale, y + r * scale, scale, scale), ink);
        }

        void DrawShopShortcut()
        {
            var rect = new Rect(1372,704,128,55);
            if (Button("상점",rect,true)) { shopOpen=true; PlaySound(6); }
            Color ink=ZeroHeroArt.Gold;
            Fill(new Rect(rect.x+15,rect.y+18,19,15),ink);
            Fill(new Rect(rect.x+12,rect.y+13,25,5),ink);
            Fill(new Rect(rect.x+17,rect.y+8,15,5),ink);
            Fill(new Rect(rect.x+18,rect.y+23,4,10),panelColor);
        }

        string RouteAdvice()
        {
            if(currentMap.counts[(int)EnemyKind.Undead]>0) return "음수 공격은 언데드에게 2배 피해. 다른 적은 회복됩니다.";
            if(currentMap.counts[(int)EnemyKind.Inverter]>0) return "반전술사는 음수 치명타에 약합니다. 다른 적은 치명타 피해가 줄어듭니다.";
            if(currentMap.counts[(int)EnemyKind.RearGuard]>0) return "방패병은 뒤에서 공격. 이동이 반전되어도 조준 방향은 유지됩니다.";
            return currentMap.detail;
        }

        void DrawTrade()
        {
            OpenSheet("능력치 교환",completedRooms==LastRoom?"마지막 교환을 마치면 던전에서 귀환합니다.":"다음: "+currentMap.name+" · "+currentMap.Roster);
            if(completedRooms<LastRoom) Text("적 특성: "+RouteAdvice(),263,278,1077,30,16,ZeroHeroArt.Gold);
            for (int i = 0; i < 3; i++)
            {
                var t = trades[i]; float y = 317 + i * 113;
                var rect = new Rect(260, y, 1080, 99);
                bool active = selected == i, hover = rect.Contains(Event.current.mousePosition);
                if (active || hover) Fill(rect, active ? C("302E23") : C("23271E"));
                Fill(new Rect(260, y + 98, 1080, 1), active ? ZeroHeroArt.Gold : lineColor);
                if (active) Marker(272, y + 27, ZeroHeroArt.Gold);
                Text((i + 1).ToString(), 302, y + 16, 58, 40, 27, active ? ZeroHeroArt.Gold : mutedColor);
                if (t.kind == TradeKind.InvertNext)
                {
                    StatIcon(t.up,381,y+25,ZeroHeroArt.Pink); Text(ZeroStats.Name(t.up),428,y+17,255,38,23,textColor);
                    Text("× −1",700,y+13,170,42,30,ZeroHeroArt.Pink,bold:true);
                    Text("다음 전투 동안만 실제 수치의 부호를 반전",875,y+21,420,35,19,mutedColor);
                    Text(Signed(StatValue(t.up))+" → "+Signed(-StatValue(t.up)),428,y+62,420,29,16,mutedColor);
                }
                else
                {
                    bool swap=t.kind==TradeKind.Swap;
                    StatIcon(t.up,381,y+25,swap?ZeroHeroArt.Gold:ZeroHeroArt.Gain); Text(ZeroStats.Name(t.up),428,y+17,165,38,23,textColor);
                    Text(swap?"교환":"+"+t.amount,599,y+13,105,40,swap?22:30,swap?ZeroHeroArt.Gold:ZeroHeroArt.Gain,bold:true);
                    Text(Signed(stats[t.up])+" → "+Signed(swap?stats[t.down]:stats[t.up]+t.amount),428,y+62,258,29,16,mutedColor);
                    StatIcon(t.down,780,y+25,swap?ZeroHeroArt.Gold:ZeroHeroArt.Pink); Text(ZeroStats.Name(t.down),826,y+17,170,38,23,textColor);
                    Text(swap?"교환":"−"+t.amount,1005,y+13,105,40,swap?22:30,swap?ZeroHeroArt.Gold:ZeroHeroArt.Pink,bold:true);
                    Text(Signed(stats[t.down])+" → "+Signed(swap?stats[t.up]:stats[t.down]-t.amount),1122,y+20,187,35,20,mutedColor,TextAnchor.UpperRight);
                    Text(swap?"두 능력치의 현재 값을 맞바꿈":t.kind==TradeKind.Extreme?"극단 교환 · 합계 변화 0":StatDetail(t.down,stats[t.down]-t.amount),826,y+62,477,28,16,t.kind==TradeKind.Extreme?ZeroHeroArt.Pink:mutedColor);
                }
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) { selected = i; PlaySound(6); }
            }
            Text(selected < 0 ? "1 / 2 / 3  선택" : trades[selected].kind==TradeKind.InvertNext?"기본 합계 유지 · 다음 전투 한정":trades[selected].kind==TradeKind.Swap?"두 값 스왑 · 합계 변화 0":"+"+trades[selected].amount+" − "+trades[selected].amount+" = 0", 264, 700, 720, 40, 22, mutedColor);
            if (Button("확정  [Enter]", new Rect(1006, 688, 334, 57), true, selected >= 0)) ApplyTrade();
        }

        void DrawPause()
        {
            Dim(.85f);
            Text("일시정지", 540, 279, 520, 66, 44, textColor, TextAnchor.MiddleCenter, true);
            if (Button("계속  [Esc]", new Rect(550, 389, 500, 59), true)) paused = false;
            if (Button("조작법", new Rect(550, 466, 500, 55))) help = true;
            if (Button("메인 메뉴", new Rect(550, 540, 500, 55))) ReturnToTitle();
            if (Button(muted ? "소리: 꺼짐" : "소리: 켜짐", new Rect(550, 614, 500, 50))) ToggleSound();
        }

    }
}
