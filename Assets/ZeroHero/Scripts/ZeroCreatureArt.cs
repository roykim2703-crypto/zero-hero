using System.Collections.Generic;
using UnityEngine;

namespace ZeroHero
{
    public sealed partial class ZeroHeroArt
    {
        public readonly Sprite[] creatures = new Sprite[13];
        public Sprite snake, wing, fly, arrow, weaponGun, weaponBlade;

        void BuildContentArt(Dictionary<char, Color> p)
        {
            p['o'] = Hex("7E9855"); p['n'] = Hex("465C35"); p['l'] = Hex("C9AA73");
            p['f'] = Hex("BB453F"); p['a'] = Hex("692C32"); p['e'] = Hex("F4E6A6");
            creatures[0] = Pixels(new[] {
                "......ssss......",".....sssss......",".....#llll#.....",".....#l#ll#.....","......llll......",".....rrrrrr.....",
                "....rlrrrrlr....","....llrrrrll....","....llssssll....",".....ssssss.....",".....ss..ss.....",".....ss..ss.....",".....##..##.....","................"
            },p);
            creatures[1] = Pixels(new[] {
                "......wwww......",".....wwwwww.....",".....w#ww#w.....",".....wwwwww.....","......w##w......",".......ss.......",
                "....wsswwssw....","....s.wssw.s....","....w.swws.w....","......ssss......","......w..w......","......s..s......",".....ww..ww.....","................"
            },p);
            creatures[2] = Pixels(new[] {
                ".....nnnnnn.....","....nooooonn....","....o#ooo#on....","....ooloolon....",".....owwwon.....","....rrrrrrrr....",
                "...orrrrrrrro...","..noorrrrrroon..","..noorrrrrroon..","....nsssssnn....",".....nn..nn.....",".....nn..nn.....","....###..###....","................"
            },p);
            creatures[3] = Pixels(new[] {
                "...n........n...","...oonnnnnnoo...","....ooooooon....","....o#ooo#on....",".....olllon.....",".....rrrrrr.....",
                "....orrorror....","....orrorror....",".....nnnnnn.....",".....nn..nn.....","....nn....nn....","....##....##...."
            },p);
            creatures[4] = Pixels(new[] {
                "......oooo......",".....oolloo.....","....lllllloo....","....l#lllloo....",".....lllloo.....",".....nnnnn......",
                "....onoonnl.....","....lnoonnl.....","....lnnnnnl.....",".....nnnnn......",".....ss.ss......",".....ss.ss......",".....##.##......","................"
            },p);
            creatures[5] = Pixels(new[] {
                ".....ssssss.....","....slllllls....","....ll#ll#ll....","....llaaaall....",".....llllll.....","...ssllllllss...",
                "..slllllllllls..",".slllllllllllls.",".slllllllllllls.","..lllrrrrrrlll..","...llrrrrrrll...","....rrrrrrrr....","....rrr..rrr....","....rrr..rrr....","...####..####...","................"
            },p);
            creatures[6] = Pixels(new[] {
                "..on..on..on.on.","..o.onoo.ono.o..","...oonoonoooo...","...onnllllnno...","....ll#ll#ll....","....llllllll....",
                ".....llllll.....","....onllllno....","...oonnnnnnoo...","..oolonnnnoloo..","..o.llnnnnll.o..","....nnnnnnnn....","....nnnnoonn....",".....nnnoonn....","....nnnoonn.....","...nnnnoonnn....","..nnnnn..nnnn...","................"
            },p);
            creatures[7] = Pixels(new[] {
                ".....eeeeee.....","...eeegggeee....","..eegwwwwgee....","..egwwwwwwge....","..egww##wwge....","..egww##wwge....",
                "..eegwwwwgee....","...eeggggee.....","....eeeeee......",".....eeee.......","......ee........","................"
            },p);
            creatures[8] = Pixels(new[] {
                ".....aa..aa.....","....aff..ffa....","....aff..ffa....","...afffafffa....","..afffffffffa...",".afffffffffffa..",
                ".affffffffffffa.","afffffafffffffa.","affffaffffafffa.","afffafffffafffa.",".affaffffffaffa.","..afffffffffaa..","...afffffffaa...","....afffffaa....",".....afffaa.....","......affa......",".......aa.......","................"
            },p);
            creatures[9] = Pixels(new[] {
                "....n......n....",".....n....n.....","....rrrrrrrr....","...rrffrrffrr...","...rfffrrfffr...","....rrrrrrrr....",
                "..ssrrrwwrrrss..",".sssrnnnnnnrsss.","..ssnnnssnnnss..","...nnnssssnnn...","...nnnssssnnn...","....nnnssnnn....","....nnnnnnnn....","...nnn....nnn...","..nn........nn..","................"
            },p);
            creatures[10] = Pixels(new[] {
                "..g..........g..","..gg........gg..","...gg..gg..gg...","...#rrrrrrrr#...","...rffrrrrffr...","...rrfrrrrfrr...",
                "....rrllllrr....","..###rrrrrr###..",".rrrr#rrrr#rrrr.","rrrrr#rrrr#rrrrr","rrr###ssss###rrr",".rr#ssssssss#rr.","...#ssssssss#...","...#ssssssss#...","...#sss..sss#...","...#sss..sss#...","...####..####...","................"
            },p);
            creatures[11] = Pixels(new[] {
                ".....ssssss.....","....ssssssss....","....sl#ll#ls....",".....llllll.....","....ssssssss....","...ssggssggss...",
                "...lssssssssl...","...lssssssssl...","....ssssssss....",".....ss..ss.....",".....ss..ss.....","....###..###...."
            },p);
            creatures[12] = Pixels(new[] {
                ".......a........","......aaa.......",".....aaaaa......","....aaaaaaa.....",".....e##ee......","......eeee......",
                "....aaaaaaaa....","...aaggaaggaa...","...aagaaagaag...","....aaaaaaaa....","...aaaaaaaaaa...","..aaaaaaaaaaaa.."
            },p);
            snake = Pixels(new[] { "....oooo","...oo#oo","oooo....","onnnn...","...nnnn.","........" },p);
            fly = Pixels(new[] { "ww....ww","www..www","..nffn..","..nnnn..",".n.nn.n.","..n..n.." },p);
            wing = Pixels(new[] { "...............w","............wwww",".........wwwwwww","......wwwwwwwwww","...wwwwwwwwwwww.","wwwwwwewwwewww..","..wwww#www#ww...","....wwwwwwww....","......wwwww.....","........ww......" },p);
            arrow = Pixels(new[] { ".........w..","ssssssssssss",".........w.." },p);
            weaponGun = Pixels(new[] { "....ssssssssssss","..ssssssssssssss","..ssggss........","...rrs..........","...rr...........","................" },p);
            weaponBlade = Pixels(new[] { "..............ww","............wwww","..........wwww..","..g.....wwww....","...g..wwww......","....ggww........","...rrgg.........","..rr..g........." },p);
        }
    }
}
