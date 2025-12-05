# ⌖ The Tale of Aamsveen ⌖

> **A short atmospheric horror game based on Dutch folk tales from the Twente region.**

<img alt="image_2025-12-05_19-40-20" src="https://github.com/user-attachments/assets/0ff137c8-63fa-479e-8a0d-d5fd8e2eb355" />


**Engine:** Unity 6 - HDRP  
**Status:** Released (Student Client Project)  
**Play on Itch.io:** [tales-of-aamsveen](https://m-nechepurenko.itch.io/tales-of-aamsveen)

---

## ✎ᝰ. About the Game

In *The Tale of Aamsveen*, you play as a father searching for his lost son in a haunted swamp. Guided only by a flickering lantern, you must find clues, avoid the creatures lurking in the fog, and survive the night.

This project was developed as a **client-based university module**. Our team worked in weekly sprints, presenting progress to client stakeholders every week to receive feedback, iterate on mechanics, and polish the atmospheric experience.

---

## 🌣 Technical Highlights

### 𖡎 Complex AI Ecosystem
The game features four distinct entity behaviors, architected using Finite State Machines and behavior trees:
* **Thimble Hunter (Lead Antagonist):** Uses volumetric line-of-sight and auditory sensors to track the player.
* **The Nixie (Water Entity):** Navigates via NavMesh on underwater surfaces. It detects when the player enters water or raises their lantern nearby. It features a unique counter-play mechanic where screaming (using the Shout input) stuns the creature.
* **The White Lady:** Spawns only when the player is not looking. She utilizes a "magnetic gaze" mechanic that pulls the player's camera towards her. Players must break eye contact by rapidly looking away or breaking line-of-sight to survive.
* **Hemanneken:** Features a swarm-like behavior system fully developed for ground-based harassment.

### ▶︎ •၊၊||၊|။ Adaptive Audio System (FMOD)
Audio is not just atmospheric but logic-driven, handled via FMOD Studio integration:
* **Dynamic Mixing:** Music intensifies based on proximity to specific threats (Hunter, Nixie, Spirit Trees).
* **Environmental Adaptation:** Footsteps change based on terrain material. Underwater states trigger a Low-Pass Filter (LPF) on all SFX and cut the music.
* **Player Feedback:** Breathing and damage sounds dynamically adapt based on current Stamina and Health values.

### ঌ Physics & Interaction
* **Lantern Mechanics:** The lantern is a physical object with inertia/sway logic. It integrates with the fuel system and interacts with enemies (stunning Hemanneken).
* **Event Systems:** A robust event architecture handles game states, saving/loading, and complex entity-environment interactions.

---

## Slam Door Interactive - Team 𐦂𖨆𐀪𖠋

This game was built by a multidisciplinary team of 7 students.

* **[Nichita Cebotari (Lead Architectural & AI Engineer)](https://linktr.ee/nikkicheb)**
    * Designed core code architecture and managed Git control (merging, QA, refactoring).
    * Developed the **Thimble Hunter AI** (FSM, Volumetric LoS) and **Nixie AI** (Underwater NavMesh navigation).
    * Programmed Player Controller and Entity-Environment interactions.
    * Co-developed the Lantern physics system.

* **[Svitlana Sosnova (Gameplay Mechanics Engineer)](https://linktr.ee/miminashca?ltsid=7c9b94a2-6f6e-431a-937d-a78485047df2)**
    * Developed the **Hemanneken AI** and **White Lady AI** (including the magnetic gaze mechanic).
    * Built the Save/Load system and Event Systems.
    * Polished Lantern physics and mechanics.

* **[Bogdan Pascari (UI/UX Designer)](https://pascaribogdan.journoportfolio.com/)**
    * Fully developed the HUD, responsive UI elements, and tutorial systems.
    * Co-developed Save/Reload logic connecting back-end systems to the UI.
    * Contributed to Level and Environment Design.

* **[Simeon Dorne (Sound Designer)](https://simeondorne.com/)**
    * Recorded and edited audio in Ableton Live; defined behaviors in FMOD Studio.
    * Wrote C# scripts to connect game logic (Stamina, Health, Zones) to FMOD parameters.
    * Created adaptive ambience that reacts to Zones and underwater states.

* **[Catalin Apostol (3D Artist - Foliage/Creatures)](https://cata1029.artstation.com)**
    * Modeled and textured the **Nixie** and **White Lady**.
    * Created all foliage assets using Speedtree.
    * Collaborated on concept art and creature design.

* **[Mariia Nechepurenko (Technical Artist)](https://www.artstation.com/mariianechepurenko)**
    * Responsible for Shaders (Swamp water, Fireflies) and VFX (Muzzle flashes).
    * Modeled the Lantern components and environmental props/landmarks.

* **[Stefani Badzheva (Character Animation Artist)](https://stefanibadzheva.artstation.com/)**
    * Modeled, textured, and rigged the **Thimble Hunter** and **Rabbits**.
    * Responsible for rigging and in-game cinematics.

---

## ⌨ Controls

| Input | Action |
| :--- | :--- |
| **W A S D** | Movement |
| **Shift** | Sprint |
| **Ctrl** | Crouch (Hides from AI) |
| **F** | Equip/Unequip Lantern |
| **RMB (Hold)** | Raise Lantern (Repels enemies, consumes fuel) |
| **H** | Shout / Call for Son |

---

## >_ Installation / How to Run

1.  Clone the repository:
    ```bash
    git clone [https://github.com/miminashca/Project-Show-Off.git](https://github.com/miminashca/Project-Show-Off.git)
    ```
2.  Open the project in **Unity 6 (HDRP)**.
3.  Ensure **FMOD** is correctly linked (if applicable for the repo version).
4.  Open `Assets/Scenes/MainMenu.unity` and press Play.

---

## ֎ AI Usage Declaration

**ChatGPT (OpenAI)** was utilized as a writing partner and debugging assistant throughout the project lifecycle. This included assistance with:
* Designing code architecture patterns.
* Debugging complex logic and refactoring for optimization.
* Formatting documentation and basic script generation.

---

*Developed at Saxion University of Applied Sciences.*
