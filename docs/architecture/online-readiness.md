# การวางระบบให้พร้อมสำหรับ Online (ทำตอนนี้ แม้ยังเล่นคนเดียว)

**เป้าหมาย:** ทำเกมให้สนุกแบบเล่นคนเดียวก่อน แต่ทุกระบบที่เขียนตอนนี้ต้องย้ายไปเป็น
**co-op 2–4 คน (hub + ดันเจี้ยนแยกห้อง)** ได้โดยไม่ต้องรื้อ

หลักใหญ่มีข้อเดียว: **แยก "สิ่งที่เกิดขึ้นจริง" (simulation) ออกจาก "สิ่งที่เห็นและได้ยิน" (presentation)**
- Simulation: ใครอยู่ตรงไหน, ใครโดนเท่าไร, ของดรอปอะไร → online แล้วเป็นหน้าที่ของ server
- Presentation: hitstop, กล้องสั่น, ตัวเลข, เสียง, particle → ทำบนเครื่องแต่ละคนได้อิสระ

---

## สถานะแต่ละจุด

| # | ระบบ | ทำไว้แล้วตอนนี้ | Online แล้วต้องทำเพิ่ม |
|---|---|---|---|
| 1 | **Input → Command** | ✅ `PlayerInputReader` แปลงปุ่มเป็น `PlayerCommands` (struct ข้อมูลล้วน) ทุกเฟรม ระบบเกมอ่านจาก struct นี้เท่านั้น | ส่ง `PlayerCommands` ไป server ทุก tick, client ทำนายผลล่วงหน้า (prediction) แล้วแก้เมื่อ server ตอบกลับ |
| 2 | **Hitstop / Slow-mo** | ✅ `CombatFeel.hitStopMode`: `GlobalTimeScale` (เล่นคนเดียว) หรือ `LocalVisual` (หยุดแค่ท่าทางของผู้ตีและผู้โดน ไม่หยุดโลก) | ใช้ `LocalVisual` ตลอด slow-mo ของ perfect dodge/คริ จะปิดเองอัตโนมัติ ถ้าอยากได้ความรู้สึกคล้ายกัน ใช้กล้องซูมหรือ vignette แทน |
| 3 | **Random (คริ, drop, ดาเมจ)** | ✅ ทุกการสุ่มผ่าน `VelaRandom` (เปลี่ยนแหล่งสุ่มได้ ใส่ seed ได้ มีเทสยืนยันว่าเล่นซ้ำได้ผลเดิม) | Server เป็นเจ้าของ seed, client ไม่สุ่มเองเลย |
| 4 | **การคำนวณดาเมจ** | ✅ ผ่าน `Health.ApplyDamage` + `DamageInfo` ทางเดียว, ฟีดแบ็กแยกไปอยู่ใน `DamageFeedback` ที่ฟัง event | ให้ `ApplyDamage` รันเฉพาะบน server แล้วส่ง `DamageInfo` ลงมาให้ client เล่นฟีดแบ็ก |
| 5 | **Perfect dodge / i-frame** | ✅ ตัดสินจาก event `Health.Evaded` + เวลาที่เริ่ม dash, มีช่อง `perfectDodgeLatencyAllowance` ไว้เผื่อ ping | Client ตัดสินก่อน (ให้รู้สึกทันที) server ตรวจยืนยันแบบยอมคลาดเคลื่อนได้ตาม allowance |
| 6 | **Loot** | ✅ `LootSpawner.Drop(..., owner)` + `WorldItem.Owner`: รองรับของแยกต่อคน | Server สุ่มแยกต่อผู้เล่น แล้วส่งให้เห็นเฉพาะเจ้าของ (ไม่แย่งของกัน) |
| 7 | **Inventory** | ✅ `Inventory` เป็น C# ล้วน ไม่ผูกกับ Unity scene มีเทสครบ | ย้ายไปอยู่ฝั่ง server ทั้งคลาส ส่วน UI แค่แสดงผลและส่งคำสั่ง (ย้าย, ใส่, ทิ้ง) |
| 8 | **Config ทั้งหมด** | ✅ เป็น ScriptableObject | Server กับ client ต้องใช้ config ชุดเดียวกัน (ตรวจเวอร์ชันตอนเชื่อมต่อ) |
| 9 | **Registry / ID** | ⚠️ `CombatRegistry` ใช้ reference ตรงๆ | เพิ่ม Entity ID (ตัวเลข) ให้ผู้เล่น, ศัตรู, ไอเทม เพื่ออ้างถึงข้ามเครื่อง |
| 10 | **เวลา (Time.time)** | ⚠️ ระบบใช้ `Time.time` / `Time.deltaTime` ของ Unity | เปลี่ยนเป็นนาฬิกาของ tick (fixed timestep) ตอนเริ่มทำ network |
| 11 | **ศัตรู AI** | ⚠️ เล็งผู้เล่นคนเดียว (`CombatRegistry.Player`) | ระบบเลือกเป้า (aggro), HP ปรับตามจำนวนคน, attack token แบ่งกันรุม |
| 12 | **Hover / pointer** | ✅ อยู่ฝั่ง presentation (ไม่เข้า simulation) | แปลงเป็นคำสั่ง "ล็อกเป้า ID x" / "เก็บไอเทม ID y" ใน `PlayerCommands` |

✅ = ทำแล้ว · ⚠️ = ยังไม่ทำ แต่รู้แล้วว่าต้องเปลี่ยนตรงไหน (ไม่ต้องรื้อระบบ)

---

## กติกาสำหรับโค้ดใหม่ (ทุก Agent ต้องทำตาม)

1. **อ่าน input จาก `PlayerCommands` เท่านั้น** ห้ามเรียก `VelaInput` ในระบบเกม (ยกเว้น UI และ hover)
2. **สุ่มผ่าน `VelaRandom`** ห้ามใช้ `UnityEngine.Random` กับสิ่งที่มีผลต่อเกม (ใช้ได้กับ particle/เสียง)
3. **ฟีดแบ็กฟัง event** ห้ามใส่เอฟเฟกต์ปนในโค้ดคำนวณ
4. **ห้ามหยุดโลกโดยตรง** ต้องขอผ่าน `HitStop.Request(seconds, participants)` เสมอ
5. **ค่าที่เกี่ยวกับจังหวะเวลา** (หน้าต่าง dodge, telegraph) ต้องมีช่องเผื่อ latency ใน config

## ทดสอบความพร้อม online ได้แล้วตอนนี้

- เปิด `Assets/Config/CombatFeel.asset` → `hitStopMode = LocalVisual` → เล่นดู
  ถ้ายังสนุกอยู่ แปลว่าความรู้สึกของ combat จะรอดตอนทำ online
- เพิ่ม `perfectDodgeLatencyAllowance = 0.1` เพื่อจำลองว่าหน้าต่าง perfect dodge จะกว้างขึ้นแค่ไหนเมื่อเผื่อ ping 100ms
