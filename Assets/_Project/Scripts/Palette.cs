using UnityEngine;

namespace ReCraft
{
    /// <summary>
    /// 아트 바이블 — 공방 환경 · 조명 · UI 색의 단일 출처.
    /// 스타일: 깔끔한 스타일라이즈드 로우폴리 PBR. 모서리를 둥글린 단순한 형태, 녹·먼지의 거친 질감과 복원된 도장의 광택 대비.
    /// 무드: 늦은 오후의 작은 공방. 왼쪽 창으로 따뜻한 햇살이 들고, 그림자는 차가운 청회색으로 되튄다. 작업 스탠드가 손 주변을 데운다.
    /// (아이템 고유 색은 각 아이템 정의(Items.cs)가 가진다 — 물건마다 원래 색이 다르기 때문.)
    /// </summary>
    public static class Palette
    {
        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;

        // 환경
        public static readonly Color Floor = Hex("#5A3E2B");       // 호두나무 마루
        public static readonly Color Wall = Hex("#5F7A70");        // 세이지 회반죽
        public static readonly Color Wainscot = Hex("#7A5236");    // 벽 아래 나무 판넬
        public static readonly Color Trim = Hex("#3B2A1E");        // 걸레받이 · 몰딩
        public static readonly Color Bench = Hex("#B07A4A");       // 꿀빛 오크 작업대
        public static readonly Color BenchDark = Hex("#6E4A2E");   // 작업대 다리 · 서랍
        public static readonly Color Mat = Hex("#2F5248");         // 커팅 매트
        public static readonly Color Tray = Hex("#3A3F46");        // 강철 부품 트레이
        public static readonly Color Steel = Hex("#B8BCC2");       // 공구 금속
        public static readonly Color Peg = Hex("#C49A6C");         // 타공판
        public static readonly Color Props = Hex("#7E8B6A");       // 선반 소품 (올리브)
        public static readonly Color PropsAlt = Hex("#A8573C");    // 선반 소품 (테라코타)
        public static readonly Color Shadow = Hex("#2B3340");      // 차가운 그림자 · 배경

        // 조명
        public static readonly Color Sun = Hex("#FFE8C8");
        public static readonly Color LampLight = Hex("#FFC98A");
        public static readonly Color Sky = Hex("#A9C6EE");
        public static readonly Color AmbientSky = Hex("#55667E");
        public static readonly Color AmbientEquator = Hex("#5A4E44");
        public static readonly Color AmbientGround = Hex("#2E241C");

        // 강조 (UI · 하이라이트)
        public static readonly Color Accent = Hex("#F2B134");
    }
}
