# Paper Shot for Apps in Toss

App name: `paper-shot`.

Build the Unity project with its installed editor first:

```bash
/home/giwan/Unity/Hub/Editor/6000.6.2f1/Editor/Unity \
  -batchmode -nographics -quit -projectPath /home/giwan/Projects/paper-shot \
  -buildTarget WebGL -executeMethod WebGLBuild.Build \
  -logFile /tmp/papershot-webgl-build.log
```

Then, from this directory:

```bash
mise exec node@26.7.0 -- npm ci
mise exec node@26.7.0 -- npm run build:ait
```

The output is `paper-shot.ait`. Upload it to the Toss console, run the
Toss-app device test, and complete the review checklist before requesting review.

The preparation script copies `Builds/WebGL`, decompresses Unity files for CDN
compatibility, and makes the canvas fill the webview. The JavaScript entrypoint
forwards native Toss back events to the Unity back handler.
