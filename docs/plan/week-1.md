# แผนสัปดาห์ที่ 1 — ทดลองทำงานกับทีม Agent

**เป้าหมายของสัปดาห์:** พิสูจน์ว่าวงรอบ "Agent สร้าง + เทส → คุณเล่น → Agent จูน" ทำงานได้จริง
โดยใช้ prototype ที่มีอยู่ตอนนี้ (Movement, Dash, Jump link, Combat, Lock-on, Skill, Loot, Inventory, Paper-doll)
แล้วเก็บค่า timing ที่พร้อมส่งให้ทีม animation

**ทีมที่ใช้สัปดาห์นี้ (5 ตัว):** Lead, Movement & Camera, Combat, QA, Tools & Build
ตัวอื่น (Enemy AI, Items, Character Visual, Level, UI) มีนิยามบทบาทไว้แล้วใน `.claude/agents/` เปิดใช้สัปดาห์ถัดไป

---

## ก่อนเริ่ม (วัน 0) — คุณทำ ~1 ชม.

- [ ] ติดตั้ง Claude Code บนเครื่องที่มี Unity (เพื่อให้ Agent รันเทส Unity แบบ batchmode ได้เอง)
- [ ] เปิดโปรเจกต์ → กด Play → ยืนยันว่าเล่นได้ (ถ้า Unity ถามให้ Rebuild ฉาก ให้กด Rebuild)
- [ ] Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All → ต้องเขียวทั้งหมด
- [ ] ดูคลิปเกมต้นแบบ จดค่าอ้างอิง 5 ค่า: ระยะ/เวลา dash, เวลาลอยตอนกระโดดข้ามน้ำ, จังหวะคอมโบ 3 ครั้ง, เวลาง้างท่าศัตรูธรรมดา, ระยะที่ไอเทมกระเด็น

## วันต่อวัน

| วัน | Agent | งาน | ผลลัพธ์ที่ตรวจได้ |
|---|---|---|---|
| **1** | Tools & Build | สคริปต์รันเทส batchmode (Win/Mac) + GitHub Actions (GameCI) รันเทสทุก PR | PR ที่มี CI เขียว |
| 1 | QA | อ่าน spec ทั้ง 3 ไฟล์ → ทำตาราง edge case ว่าข้อไหนเทสอัตโนมัติ ข้อไหนต้องเล่นเอง | `docs/playtest/week-1-checklist.md` |
| **2** | Movement | PlayMode tests: ระยะ dash, i-frame, jump link (มุม/ระยะ), walk-to ถูกยกเลิก | เทสใหม่ผ่าน |
| 2 | Combat | PlayMode tests: combo buffer/reset, skill cooldown, lock-on หลุดเมื่อไกล/ตาย, คลิก UI ไม่โจมตี | เทสใหม่ผ่าน |
| **3** | คุณ | เล่น 15 นาทีตาม checklist → จดสิ่งที่รู้สึก เป็นตัวเลขถ้าได้ ("dash ไกลไป ~20%") | โน้ตใน issue |
| 3 | Lead | แปลงโน้ตเป็น task เล็กๆ แจก Movement / Combat | task list |
| **4** | Movement + Combat | จูนค่าตามโน้ต (แก้ config เท่านั้น) + อัปเดตตาราง Tuning ใน spec | PR config |
| 4 | Combat | ทำ `docs/frame-data.md` — ทุกท่า windup/active/recovery เป็นเฟรม @60fps | ไฟล์ frame data |
| **5** | คุณ + Lead | เล่นรอบสุดท้าย → ล็อกค่า → สรุปว่า workflow แบบนี้ใช้ได้ไหม ควรเพิ่ม Agent ตัวไหน | สรุปสัปดาห์ |

## วงรอบประจำวัน

```
เช้า     คุณ/Lead เลือก task ของวัน (1 task = 1 branch = 1 PR)
กลางวัน  Agent ทำงานคู่ขนานใน branch ของตัวเอง → เขียนเทส → รันเทส → เปิด PR
         QA รันเทสทั้งหมด → รายงานสิ่งที่พัง
เย็น     คุณเล่น 15 นาที → จดความรู้สึก → Lead แปลงเป็น task พรุ่งนี้
```

วิธีสั่งงาน Agent ใน Claude Code (ตัวอย่าง):

```
> ใช้ agent movement-camera: เขียน PlayMode test สำหรับ jump link ตาม docs/specs/movement-dash-jump.md
  แล้วรันเทสทั้งหมด เปิด PR เมื่อผ่าน
```

## Checklist เล่นทดสอบ (15 นาที)

1. **เดิน / หัน 4 ทิศ:** เดินวน สังเกตว่าตัวละครเปลี่ยนภาพหน้า/หลัง/ข้าง ถูกจังหวะไหม
2. **Dash:** dash ผ่าน telegraph ของ slime → รู้สึกปลอดภัยไหม ไกล/ใกล้ไป?
3. **กระโดดข้ามน้ำ:** เดินไปที่หินกลางแม่น้ำ (x=0 หรือ x=-14) → Space ข้าม → ลอยนาน/สั้นไป?
4. **Lock-on:** Q ล็อก, E เปลี่ยนเป้า, คลิกมอนสเตอร์ → เป้าชัดไหม
5. **สกิล:** กด 1–4 → เท่ไหม cooldown นาน/สั้นไป? ลองเปลี่ยนปุ่มเมาส์ใน inventory (I)
6. **Loot:** ฆ่า Brute (ดรอป Rare) → ของเด้งสวยไหม แสงแยกระดับชัดไหม
7. **Inventory เต็ม:** กด F5 หลายๆ ครั้งจนกระเป๋าเต็ม → คลิกเก็บ → ข้อความชัดไหม
8. **ใส่ของ:** ลากหมวก/เกราะไปช่องสวมใส่ → ตัวละครเปลี่ยนทันทีไหม, กด O สุ่มชุด
9. **Crit / BREAK:** ใช้ดาบใหญ่ตี Brute → เห็นความต่างท่าเบา-หนักไหม

จดโน้ตแบบ: `[ระบบ] สิ่งที่รู้สึก → อยากให้เป็นแบบไหน (ถ้ามีตัวเลขยิ่งดี)`

---

## โรดแมปหลังสัปดาห์ 1 (ร่าง)

| สัปดาห์ | โฟกัส | Agent ที่เพิ่ม |
|---|---|---|
| 2 | Enemy encounter + Items: ตารางดรอปสมดุล, stat ของอุปกรณ์, save/load | Enemy AI, Items |
| 3 | Art pipeline: importer Aseprite, animation หลายเฟรมต่อทิศ, UI จริง (UGUI/UI Toolkit) | Character Visual, UI |
| 4 | Level จริง 1 ด่าน: NPC, สิ่งของโต้ตอบ, เส้นทางกระโดดหลายจุด | Level |

## ความเสี่ยงที่ต้องเฝ้าดู

- **Agent หลายตัวแก้ไฟล์เดียวกัน** → กติกาเจ้าของโฟลเดอร์ใน `.claude/agents/`; ฉาก (`.unity`) มีเจ้าของคนเดียว (Level)
- **"ฟีล" ตัดสินโดย Agent ไม่ได้** → ทุกค่า feel อยู่ใน config + ตาราง Tuning ใน spec ให้คุณตัดสิน
- **เทส PlayMode ช้า/เปราะ** → เทสเฉพาะกติกาที่ต้องถูกเสมอ ส่วนความสวยงามใช้ checklist
