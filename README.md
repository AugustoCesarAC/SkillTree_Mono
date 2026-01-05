# **Skill Tree Mod**

**Source Code**: [https://github.com/AugustoCesarAC/SkillTree](https://github.com/AugustoCesarAC/SkillTree)

**Main and Mono Branch**

## **Functions**

To open the Skill Tree Window, just press **"C"**

When a player levels up (increases Tier or Rank), they earn Skill Points:

* 1 Point for every Tier increase.
* 2 Points for every Rank increase.

*Example: If you are Tier V and advance to Tier I, you will receive 2 points.*

Skill Points are divided into three categories: **Stats, Operations, and Social.**

* **Stats**: Modify the player's base attributes, such as Max Health, XP gain, and Movement Speed.
* **Operations**: Relates to plant cultivation and Chemistry Stations.
* **Social**: Affects NPC interactions and player status.

---

## **Skill Trees (Spoilers)**

### **Category: Stats**

```text
[*] [More Health]: Increase Max Health by +20 (Parent: None)
    [*] [More Movespeed]: Increase 10% by Movespeed (Parent: More Health)
    [*] [More XP]: Increase XP Gain by 5% (Parent: More Health)
        [*] [More Item Stack (x2)]: Increase Item Stack (x2) (Parent: More XP)
        [*] [More XP Per Sell When Earn Money]: Earn 5% bonus XP (Parent: More XP)
        [*] [More XP 2]: Increase XP Gain by an additional 5% (Parent: More XP)
    [*] [Better Delivery]: Make Deliveries More Fast (6H -> 2H) (Parent: More Health)
    [*] [Allow Sleep with Athletic or Energizing]: (Parent: More Health)
        [*] [Allow Use Bed to Skip Schedule]: Skip Schedule (Parent: Allow Sleep)
    [*] [Counteroffer Perception]: See chance of counteroffer (Parent: More Health)

```

### **Category: Operations**

```text
[*] [Better Grow Tent Quality]: Trash -> Low (Parent: None)
    [*] [Increase Growth Speed]: Increase Growth Speed by 2.5% (Parent: Better Grow Tent)
        [*] [More Yield]: Increase Yield by 1 (Parent: Increase Growth Speed)
            [*] [Advanced Pot Techniques]: +15% Quality & Pot Tiers (Parent: More Yield)
                [*] [More Quality Mushroom]: Upgrade Tier (Parent: Advanced Pots)
                [*] [Increase Growth Speed 2°]: +2.5% Speed (Parent: Advanced Pots)
            [*] [More Quality Meth]: Upgrade Meth Quality Tier (Parent: More Yield)
            [*] [Chemist Station Quick]: Increase Station Speed (Parent: More Yield)
                [*] [More Mix and Drying Rack Output]: Double Output (Parent: Chemist)
                    [*] [More Cauldron Output]: Double Output (Parent: Mix/Drying)
                [*] [AbsorbentSoil]: Preserve soil additives (Parent: Chemist)

```

### **Category: Social**

```text
[*] [More sample chance]: Increase Sample Chance by 5% (Parent: None)
    [*] [Civil More Money per week]: Increase Weekly Money by 10% (Parent: Marketing)
    [*] [More ATM Limit]: Increase ATM Deposit Limit by +1500 (Parent: Marketing)
    [*] [Better Business]: +20% Max Laundering Capacity (Parent: Marketing)
    [*] [Better Supplier]: +50% Debt and Item Limits (Parent: Marketing)
    [*] [Dealer More Customer]: Increase Dealer Customers (+2) (Parent: Marketing)
        [*] [Less Dealer Cut]: Decrease Dealer's Cut by 5% (Parent: Dealer)
        [*] [Dealer Speed Up]: Increase Movespeed (2x) (Parent: Dealer)

```

---

## **Base Feature Changes**

### **Plant Quality & Pots**

Plant quality is now determined by the quality of the pot used.

* **Base Grow Tent**: Trash Quality -> (The first skill in "Operations" increases this to Low).
* **Plastic Pot**: Low Quality.
* **Moisture-Preserving Pot**: Low Quality -> (Skill "More Quality" increases this to Moderate).
* **Air Pot**: Moderate Quality.

### **Hardware Store Changes**

Items added to hardware stores with a **250% base price increase** (Convenience Tax).

* Drying Rack (250 -> 400)
* Moisture Preserving Pot, LED Grow Light, Plastic Pot, Halogen Grow Light, Suspension Rack, Air Pot.

### **Rank Requirements**

Certain items now require higher ranks. Example: *Drying Rack requires Street Rat V.*

### **Business Payments**

Payments are now sent every **4 hours**. (e.g., Laundering 2,000 pays out 334 six times a day).

---

## **Installation**

1. **Verify Branch**: Steam -> Right-click "Schedule I" -> Properties -> Betas -> Select **"Alternate"**. Wait for the update.
2. **MelonLoader**: Install from [https://melonwiki.xyz/](https://melonwiki.xyz/).
3. **Plugin**: Drop *SkillTree.dll* into the *Schedule I/Mods* folder.

---

## **Inspirations**

* **S1API (Fork)**: [https://github.com/ifBars/S1API](https://github.com/ifBars/S1API)
* **QualityPlantsMod**: [https://github.com/Soul-Da-Sythe/QualityPlantsMod](https://github.com/Soul-Da-Sythe/QualityPlantsMod)
* **ScheduleOneNewGamePlus**: [https://github.com/regularberry/ScheduleOneNewGamePlus](https://github.com/regularberry/ScheduleOneNewGamePlus)
* **ProduceMore**: [https://github.com/lasersquid/Sched1ProduceMoreMod/tree/master](https://github.com/lasersquid/Sched1ProduceMoreMod/tree/master)
* **Absorbent Soil**: [https://www.nexusmods.com/schedule1/mods/843](https://www.nexusmods.com/schedule1/mods/843)
* **Wolf's Business Improvements**: [https://www.nexusmods.com/schedule1/mods/526](https://www.nexusmods.com/schedule1/mods/526)
* **BetterCounterOffer**: [https://github.com/OvrwghtUnicorn/BetterCounterOffer/tree/main](https://github.com/OvrwghtUnicorn/BetterCounterOffer/tree/main)

---

## **Observations**

**Multiplayer:** I haven't been able to test this thoroughly in multiplayer yet. There may be bugs, and I will do my best to fix them as they are reported.

**Feedback:** If you encounter bugs, have balance suggestions, or new ideas, please notify me on the mod page.

**Open Source:** The code is open source so anyone can modify or learn from it. (I used AI to help with patching, but I manually handle the logic and code insertion when the AI struggles).

---

## **Known Issues**

(I have tried my best to fix these, but they remain minor issues for now):

* The Better Supplier interface does not update the display from 0/10 to 0/20, but you can still successfully buy 20 items.
* While the Skill Tree UI is open, you can still perform punches (but you cannot use items).
* When increasing the "Max Assigned Customers" for dealers, the UI lacks a scrollbar, but customers are assigned successfully.
* If you start the mod during the Tutorial, the Skill Tree UI will bug out.
* If you exit to the menu and restart without closing the game entirely, the mod may trigger an error.

---

## **Curiosity**

This isn't my first mod, but it is the first one I've made for myself that I felt was "good enough" to share. One of my previous mods, City Evolving, was actually integrated into this skill tree. My other previous work was a "Wait" system similar to Bethesda games.

---

## **F.A.Q.**

**1. Q: My points disappeared or my skills reset. Why?**
A: This happens if the JSON file and your save go out of sync (e.g., closing without saving). The mod safely resets skills and returns your points.

---

<img width="213" height="217" alt="Paypal" src="https://github.com/user-attachments/assets/09358efc-bc2c-4863-9706-b47e22b7e34c" />

If you liked it, please **endorse** this mod and, if you're feeling generous, consider **buying me a coffee**! ☕
