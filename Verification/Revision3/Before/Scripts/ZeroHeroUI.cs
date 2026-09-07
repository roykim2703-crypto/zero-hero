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
                if (phase == Phase.Combat) DrawCombat();
                if (phase == Phase.Trade) DrawTrade();
                if (phase == Phase.Camp) DrawCamp();
                if (phase == Phase.Dead || phase == Phase.Victory) DrawEnd();
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
            Text(best > 0 ? "최고 기록  " + best + " / 8 방" : "8개의 방 · 죽으면 처음부터", 136, 816, 690, 32, 17, mutedColor);
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
            else rows = new[] { "..####..", ".##..##.", "##.##.##", "##.#..##", "##..#.##", "##.##.##", ".##..##.", "..####.." };
            for (int r = 0; r < rows.Length; r++) for (int c = 0; c < rows[r].Length; c++)
                if (rows[r][c] == '#') Fill(new Rect(x + c * scale, y + r * scale, scale, scale), ink);
        }

        void DrawHUD()
        {
            Fill(new Rect(0, 0, 1600, 116), C("141613"));
            Text("체력", 54, 29, 70, 29, 19, mutedColor);
            for (int i = 0; i < 20; i++)
            {
                var r = new Rect(120 + i * 13, 35, 10, 19);
                Fill(r, C("35362C"));
                if (hp > i * 5) Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01((hp - i * 5) / 5), r.height), hp <= 25 ? ZeroHeroArt.Pink : C("B57960"));
            }
            Text(Mathf.CeilToInt(hp) + "/100", 402, 27, 175, 37, 24, textColor, bold: true);
            Text(shields > 0 ? "보호막 " + shields : "", 55, 78, 240, 26, 16, ZeroHeroArt.Gain);
            Text(room + " / 8", 670, 20, 260, 42, 30, textColor, TextAnchor.MiddleCenter, true);
            for (int i = 0; i < 8; i++) Fill(new Rect(704 + i * 25, 78, 17, 5), i + 1 == room ? ZeroHeroArt.Gold : i + 1 < room ? C("8C8C69") : C("373C30"));
            StatIcon(3, 1112, 31, ZeroHeroArt.Gold);
            Text(wallet.ToString(), 1152, 24, 143, 41, 28, ZeroHeroArt.Gold);
            if (Button(phase == Phase.Combat ? "일시정지" : "조작법", new Rect(1344, 27, 202, 46))) { if (phase == Phase.Combat) paused = true; else help = true; }
            Text(TimeText(runTime) + "   ·   " + kills + " 처치", 1130, 80, 416, 25, 15, mutedColor, TextAnchor.UpperRight);
            DrawStatBar();
        }

        void DrawStatBar()
        {
            Fill(new Rect(0, 791, 1600, 109), C("141613"));
            Fill(new Rect(54, 792, 1492, 2), lineColor);
            for (int i = 0; i < 4; i++)
            {
                float x = 54 + i * 310; Color ink = stats[i] < 0 ? ZeroHeroArt.Pink : textColor;
                StatIcon(i, x, 819, ink);
                Text(ZeroStats.Name(i), x + 41, 811, 167, 32, 18, mutedColor);
                Text(Signed(stats[i]), x + 191, 806, 85, 46, 30, ink, TextAnchor.UpperRight, true);
                Text(StatDetail(i, stats[i]), x + 41, 855, 253, 30, 14, stats[i] <= 0 ? ZeroHeroArt.Pink : mutedColor);
            }
            Text("총합 " + stats.Total, 1336, 812, 210, 35, 22, textColor, TextAnchor.UpperRight);
            Text("변화량 " + (stats.Total - ZeroStats.InitialTotal), 1336, 855, 210, 26, 15, mutedColor, TextAnchor.UpperRight);
        }

        string StatDetail(int index, int value)
        {
            if (index == 0) return value < 0 ? "적 체력 " + -value * 6 + " 회복" : "한 발당 피해 " + value * 6;
            if (index == 1) return value < 0 ? "받는 피해 +" + -value * 2 : "받는 피해 −" + value * 2;
            if (index == 2) return value == 0 ? "이동 불가 · 회피 가능" : (value < 0 ? "반대로 " : "이동 ") + (Mathf.Abs(value) * 1.6f).ToString("0.0") + " m/s";
            return value < 0 ? "동전당 " + -value + " G 잃음" : "동전당 " + value + " G";
        }

        void DrawCombat()
        {
            Text("남은 적 " + (enemies.Count + pendingSpawns), 58, 135, 280, 30, 18, textColor);
            Text(autoAim ? "자동 조준  [Q]" : "마우스 조준  [Q]", 1220, 135, 322, 30, 16, mutedColor, TextAnchor.UpperRight);
            foreach (var e in enemies)
            {
                if (e.kind != 3) continue;
                Text(room == 8 ? "왕" : "문지기", 570, 134, 460, 30, 21, ZeroHeroArt.Gold, TextAnchor.MiddleCenter);
                Fill(new Rect(570, 176, 460, 7), C("44352A")); Fill(new Rect(570, 176, 460 * e.hp / e.maxHp, 7), ZeroHeroArt.Pink);
            }
            if (roomBanner > 0) Text(room + "번째 방", 570, 229, 460, 54, 34, new Color(textColor.r, textColor.g, textColor.b, Mathf.Min(1, roomBanner)), TextAnchor.MiddleCenter, true);
            foreach (var f in floating)
            {
                Vector3 screen = cam.WorldToScreenPoint(f.pos);
                float x = (screen.x - guiOffsetX) / guiScale, y = (Screen.height - screen.y - guiOffsetY) / guiScale;
                var c = f.color; c.a = Mathf.Min(1, f.life * 2);
                Text(f.text, x - 70, y - 25, 140, 38, 20, c, TextAnchor.MiddleCenter, true);
            }
            if (noticeTime > 0) Text(notice, 315, 742, 970, 34, 17, ZeroHeroArt.Gold, TextAnchor.MiddleCenter);
            Ability("Shift", "회피", dashTimer, 1.35f, 57, 723);
            Ability("E", "폭탄", bombTimer, 7, 1344, 723);
        }

        void Ability(string key, string name, float cooldown, float max, float x, float y)
        {
            Fill(new Rect(x - 7, y - 4, 211, 55), new Color(.075f, .08f, .065f, .9f));
            Text(key, x, y + 2, 69, 30, 18, cooldown > 0 ? mutedColor : textColor, bold: true);
            Text(cooldown > 0 ? cooldown.ToString("0.0") + "초" : name, x + 72, y + 2, 125, 30, 18, cooldown > 0 ? mutedColor : textColor);
            Fill(new Rect(x, y + 40, 193, 3), C("363A2E"));
            Fill(new Rect(x, y + 40, 193 * (1 - Mathf.Clamp01(cooldown / max)), 3), ZeroHeroArt.Gold);
        }

        void OpenSheet(string title, string description)
        {
            Fill(new Rect(0, 116, 1600, 675), new Color(.065f, .07f, .055f, .97f));
            Text(title, 260, 161, 1080, 65, 42, textColor, bold: true);
            Text(description, 263, 243, 1074, 45, 19, mutedColor);
        }

        void DrawTrade()
        {
            OpenSheet("능력치 교환", room + "번째 방 클리어. 하나를 골라야 다음으로 넘어갑니다.");
            for (int i = 0; i < 3; i++)
            {
                var t = trades[i]; float y = 317 + i * 113;
                var rect = new Rect(260, y, 1080, 99);
                bool active = selected == i, hover = rect.Contains(Event.current.mousePosition);
                if (active || hover) Fill(rect, active ? C("302E23") : C("23271E"));
                Fill(new Rect(260, y + 98, 1080, 1), active ? ZeroHeroArt.Gold : lineColor);
                if (active) Marker(272, y + 27, ZeroHeroArt.Gold);
                Text((i + 1).ToString(), 302, y + 16, 58, 40, 27, active ? ZeroHeroArt.Gold : mutedColor);
                StatIcon(t.up, 381, y + 25, ZeroHeroArt.Gain);
                Text(ZeroStats.Name(t.up), 428, y + 17, 165, 38, 23, textColor);
                Text("+" + t.amount, 599, y + 13, 80, 40, 30, ZeroHeroArt.Gain, bold: true);
                Text(Signed(stats[t.up]) + " → " + Signed(stats[t.up] + t.amount), 428, y + 62, 258, 29, 16, mutedColor);
                StatIcon(t.down, 780, y + 25, ZeroHeroArt.Pink);
                Text(ZeroStats.Name(t.down), 826, y + 17, 170, 38, 23, textColor);
                Text("−" + t.amount, 1005, y + 13, 85, 40, 30, ZeroHeroArt.Pink, bold: true);
                Text(Signed(stats[t.down]) + " → " + Signed(stats[t.down] - t.amount), 1122, y + 20, 187, 35, 20, mutedColor, TextAnchor.UpperRight);
                Text(StatDetail(t.down, stats[t.down] - t.amount), 826, y + 62, 477, 28, 16, stats[t.down] - t.amount <= 0 ? ZeroHeroArt.Pink : mutedColor);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) { selected = i; PlaySound(6); }
            }
            Text(selected < 0 ? "1 / 2 / 3  선택" : "+" + trades[selected].amount + " − " + trades[selected].amount + " = 0", 264, 700, 620, 40, 22, mutedColor);
            if (Button("확정  [Enter]", new Rect(1006, 688, 334, 57), true, selected >= 0)) ApplyTrade();
        }

        void DrawCamp()
        {
            OpenSheet("상점", ZeroStats.Name(lastUp) + " +" + lastAmount + "   " + ZeroStats.Name(lastDown) + " −" + lastAmount + "   적용됨");
            Text("물약", 291, 334, 400, 44, 30, textColor, bold: true);
            Text("체력 40 회복", 291, 391, 650, 35, 21, mutedColor);
            string healLabel = campHealed ? "구매 완료" : hp >= 100 ? "체력 최대" : wallet < 24 ? "24 G · 돈 부족" : "24 G  구매";
            if (Button(healLabel, new Rect(1006, 345, 334, 58), false, !campHealed && wallet >= 24 && hp < 100))
            { wallet -= 24; hp = Mathf.Min(100, hp + 40); campHealed = true; PlaySound(4); }
            Fill(new Rect(263, 452, 1077, 1), lineColor);
            Text("보호막", 291, 486, 400, 44, 30, textColor, bold: true);
            Text("다음 공격 1회 막기", 291, 543, 650, 35, 21, mutedColor);
            string shieldLabel = campShield ? "구매 완료" : wallet < 18 ? "18 G · 돈 부족" : "18 G  구매";
            if (Button(shieldLabel, new Rect(1006, 496, 334, 58), false, !campShield && wallet >= 18))
            { wallet -= 18; shields++; campShield = true; PlaySound(6); }
            Fill(new Rect(263, 605, 1077, 1), lineColor);
            Text("각 물품은 한 번씩 살 수 있습니다.", 264, 702, 710, 34, 18, mutedColor);
            if (Button("다음 방", new Rect(1006, 688, 334, 57), true)) NextRoom();
        }

        void DrawEnd()
        {
            bool won = phase == Phase.Victory;
            OpenSheet(won ? "던전 돌파" : "사망", won ? "8개 방을 모두 클리어했습니다." : room + "번째 방에서 쓰러졌습니다.");
            string[] labels = { "클리어", "처치", "시간" };
            string[] values = { (won ? 8 : room - 1) + " / 8 방", kills + "마리", TimeText(runTime) };
            for (int i = 0; i < 3; i++)
            {
                float x = 278 + i * 364;
                Text(labels[i], x, 367, 310, 33, 18, mutedColor);
                Text(values[i], x, 417, 310, 66, 39, textColor, bold: true);
            }
            Fill(new Rect(263, 525, 1077, 1), lineColor);
            if (Button("다시 시작  [Enter]", new Rect(263, 581, 515, 63), true)) StartRun();
            if (Button("메인 메뉴", new Rect(805, 581, 535, 63))) ReturnToTitle();
            Text("최고 기록 " + best + " / 8 방", 264, 704, 800, 30, 18, mutedColor);
        }

        void ReturnToTitle()
        {
            ClearActors(); phase = Phase.Title; paused = false; help = false; player = new Vector2(6, -.25f);
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

        void DrawHelp()
        {
            Dim(.985f);
            Text("조작법", 130, 80, 1000, 70, 44, textColor, bold: true);
            string[] keys = { "WASD / 방향키", "마우스", "클릭 / Space", "Shift", "E", "Q", "Esc / M / F1" };
            string[] actions = { "이동", "조준", "연속 공격", "회피 · 1.35초마다", "폭탄 · 고정 피해 42 · 7초마다", "자동 조준 전환", "일시정지 / 소리 / 조작법" };
            for (int i = 0; i < keys.Length; i++)
            { Text(keys[i], 130, 205 + i * 55, 255, 37, 21, ZeroHeroArt.Gold); Text(actions[i], 395, 205 + i * 55, 430, 37, 21, textColor); }
            Text("능력치가 음수라면", 890, 205, 570, 40, 25, textColor, bold: true);
            string[] negatives = { "공격: 적 체력을 회복시킴", "방어: 받는 피해가 늘어남", "이동: 입력 반대로 이동함", "자금: 동전을 주우면 돈을 잃음" };
            for (int i = 0; i < negatives.Length; i++) Text(negatives[i], 890, 278 + i * 51, 570, 35, 21, ZeroHeroArt.Pink);
            Text("이동이 반전돼도 공격 방향은 그대로입니다.\n공격력이 0 이하라면 폭탄으로 싸우세요.\n이동 속도 0에서도 회피는 가능합니다.", 890, 522, 570, 130, 18, mutedColor);
            Text("방을 깨면 남은 동전이 자동으로 수집됩니다.\n이때도 음수 자금 획득 효과가 적용됩니다.", 130, 644, 700, 73, 18, mutedColor);
            Text("능력치 총합은 10. 모든 교환의 변화량은 0.\n받는 피해는 최소 1, 보유금은 최소 0입니다.", 890, 665, 570, 70, 18, mutedColor);
            if (Button("닫기  [Esc]", new Rect(1120, 792, 340, 57), true)) { help = false; paused = false; }
        }
    }
}
