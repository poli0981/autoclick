# Disclaimer

**Last updated:** April 2026

## General Disclaimer

AutoClick is provided "as is" and "as available" without any warranties of any kind, either express or implied, including but not limited to the implied warranties of merchantability, fitness for a particular purpose, or non-infringement.

## System Requirements & Multi-Game Usage

### Hardware Requirements

AutoClick supports running multiple games simultaneously. **This places significant additional load on your system.** Before using AutoClick with multiple games, you MUST:

- **Verify that your system meets or exceeds the minimum hardware requirements specified by each game's developer.** These requirements are typically listed on the game's store page (Steam, Epic Games, etc.) or official website.
- **Account for cumulative resource usage.** Running 2 or more games simultaneously requires substantially more CPU, RAM, GPU memory, and disk I/O than running a single game. The combined requirements may far exceed the sum of individual minimums.
- **Monitor system temperatures.** Sustained multi-game operation can cause elevated CPU and GPU temperatures. Ensure adequate cooling and ventilation.

### Developer Responsibility

**THE DEVELOPER (poli0981) IS NOT RESPONSIBLE FOR:**

- **Hardware damage**, including but not limited to: overheating, thermal throttling, component degradation, power supply failures, or any physical damage to your computer or peripherals caused by running multiple games or applications simultaneously.
- **System instability**, including but not limited to: crashes, freezes, blue screens (BSOD), data corruption, or operating system errors resulting from excessive resource usage.
- **Data loss** of any kind, including but not limited to: unsaved game progress, corrupted save files, lost documents, or filesystem damage caused by system crashes during multi-game operation.
- **Performance degradation** of other applications or system services running alongside AutoClick and multiple games.
- **Electricity costs or power consumption** associated with sustained multi-game operation.

### User Responsibility

It is YOUR sole responsibility to:

1. Ensure your hardware is adequate for the number of games you intend to run simultaneously
2. Monitor system resource usage (CPU, RAM, GPU, temperatures) during operation
3. Stop AutoClick and/or close games if you observe signs of system stress (high temperatures, excessive fan noise, sluggish performance)
4. Maintain proper backups of important data before running resource-intensive operations
5. Ensure adequate power supply and cooling for sustained operation

## Game Compatibility

- **Not all games are compatible.** AutoClick uses Win32 PostMessage API to simulate mouse clicks. Some games may not respond to this method due to their input handling implementation.
- **DirectInput / Raw Input games** may not detect PostMessage-based clicks.
- **Fullscreen exclusive mode** games may have limited compatibility.
- The developer makes no guarantees that AutoClick will work with any specific game.

## Anti-Cheat Warning

**USE AT YOUR OWN RISK WITH ANTI-CHEAT PROTECTED GAMES.**

- Many online games employ anti-cheat systems (e.g., EasyAntiCheat, BattlEye, Vanguard, GameGuard, nProtect) that actively scan for and detect automation tools.
- **Kernel-level anti-cheat systems** are particularly aggressive and may detect AutoClick even when it runs as a separate process.
- Using AutoClick with anti-cheat protected games **may result in**:
  - Temporary or permanent account bans
  - Loss of in-game progress and purchases
  - Hardware ID bans
- **The developer (poli0981) assumes NO responsibility for any account bans, suspensions, or losses incurred from using this software.**
- It is your sole responsibility to verify whether automation tools are permitted under a game's Terms of Service before use.

## Automated Operation & Unattended Use

AutoClick includes features designed for unattended operation (scheduler, auto-stop, AFK notifications). When using these features:

- **Do not leave your system unattended for extended periods** without first verifying that temperatures and resource usage are within safe limits.
- **The scheduler and auto-stop features are provided as conveniences** and are not guaranteed to function perfectly in all scenarios. System crashes, power outages, or unexpected game behavior may prevent scheduled stops from executing.
- **You are responsible for any consequences** of unattended operation, including but not limited to: game Terms of Service violations, excessive resource usage, and hardware stress.

## AI Assistance Disclosure

- This software was developed with the assistance of **AI tools** (Claude by Anthropic) during the coding process.
- **In-app translations** (Vietnamese, Japanese, Korean, Chinese) are **AI-generated** and may not be 100% accurate. If you find translation errors, please report them via GitHub Issues.
- AI-assisted development does not diminish the quality standards applied to the code, but users should be aware of this methodology.

## Limitation of Liability

In no event shall the developer be liable for any direct, indirect, incidental, special, consequential, or exemplary damages, including but not limited to:

- Loss of game accounts or in-game items
- Damage to hardware or software
- Loss of data or profits
- Business interruption
- Hardware damage from overheating or excessive resource usage
- Electrical costs or power consumption
- System instability or data corruption

arising out of the use or inability to use the software, even if the developer has been advised of the possibility of such damages.

## No Legal Advice

This disclaimer does not constitute legal advice. Users are responsible for ensuring their use of AutoClick complies with all applicable laws and third-party terms of service.
