cd "out/build/x64-Release/app/"
app.exe -rep 50 -seed 0 -x 256 -y 256 -name seed0 -size 78184 -num_mountain_agents 4 -num_beach_agents 8 -num_smooth_agents 4 -num_hill_agents 4 -mountain_tokens 32 -beach_tokens 64 -smooth_tokens 16 -hill_tokens 32
cd "../../../../"