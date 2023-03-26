import shutil
from pathlib import Path

SRC = Path(__file__).parent
RELEASE = SRC / "_release"
GAME_DATA = SRC / "Resources" / "GameData" / "MagicSmokeIndustries"

def patch_tweakscale_part_mass(root: Path, target: float):
    tweakscale_cfg = root / "Parts" / "Rework_TweakScale.cfg"
    
    tweakscale_lines = tweakscale_cfg.read_text().splitlines(keepends=True)
    
    with tweakscale_cfg.open("w") as f:
        for line in tweakscale_lines:
            if line.lstrip().startswith("mass ="):
                prefix = line[:line.index("mass")]
                line = f"{prefix}mass = {target:.2f}\n"
            f.write(line)

def main():
    RELEASE.mkdir(exist_ok=True)
    # Clear release folder
    shutil.rmtree(RELEASE)
    
    # Copy resource files
    shutil.copytree(GAME_DATA, RELEASE)
    
    # Patches
    patch_tweakscale_part_mass(RELEASE, 0.16)
    
    # Copy RO config
    shutil.copy(SRC / "RO.cfg", RELEASE / "RO.cfg")
        

if __name__ == "__main__":
    main()