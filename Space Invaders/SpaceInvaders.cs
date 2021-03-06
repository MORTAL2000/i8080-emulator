﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using eZet.i8080.Diagnostics;
using eZet.i8080.Emulator;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace eZet.i8080.Games.SpaceInvaders {
    public partial class SpaceInvaders : Form, IVideoDevice {


        private System8080 system;

        private Debugger debug;

        private BackgroundWorker bw;

        private Bitmap bmap;

        public unsafe SpaceInvaders() {
            InitializeComponent();
            initializeCpu();
        }

        public void vblank() {
            if (pictureBox1.InvokeRequired && !this.IsDisposed) {
                pictureBox1.Invoke(new MethodInvoker(refresh));
            } else {
                refresh();
            }
        }

        private unsafe void refresh() {
            var ram = system.getVram();
            Array.Reverse(ram);
            GCHandle handle = GCHandle.Alloc(ram, GCHandleType.Pinned);
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(ram, 0);
            bmap = new Bitmap(256, 224, 32, PixelFormat.Format1bppIndexed, ptr);
            handle.Free();
            bmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            pictureBox1.Image = bmap;
            pictureBox1.Refresh();
            //BitmapData result = bmap.LockBits(new Rectangle(0, 0, 256, 224), ImageLockMode.ReadWrite, PixelFormat.Format1bppIndexed);
            //bmap.UnlockBits(result);
            //pictureBox1.Image = null;
        }

        private void initializeCpu() {
            system = new System8080();
            debug = new Debugger(system);

            system.addOutput(new DebugDevice(6), 6);
            system.addOutput(new DebugDevice(3), 3);
            system.addOutput(new DebugDevice(5), 5);
            var controller = new ArcadeController();
            system.addInput(controller, 1);
            system.addInput(new DebugDevice(0, true, 0xff), 0);
            //system.addInput(new DebugDevice(1, false, 0x81), 1);
            system.addInput(new DebugDevice(2, false, 0x83), 2);

            this.KeyDown += new KeyEventHandler(controller.KeyDown);
            this.KeyUp += new KeyEventHandler(controller.KeyUp);


            var shift = new ShiftDevice();
            system.addInput(shift, 3);
            system.addOutput(shift, 2);
            system.addOutput(shift, 4);

            system.AddVideo(this);


            MemoryStream ms = loadInvaders();
            system.loadProgram(ms, 0);

            startInterruptTimer();

            // boot i8080
            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerAsync(system);

        }

        private void bw_DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker worker = sender as BackgroundWorker;
            System8080 system = e.Argument as System8080;
            system.boot();
        }

        private void startInterruptTimer() {
            var t = new System.Windows.Forms.Timer();
            t.Interval = 14;
            t.Tick += (source, e) => {
                system.IoController.Interrupt(0, 0xcf); // RST 8 
                Thread.Sleep(8);
                system.IoController.Interrupt(0, 0xd7); // RST 10 start vblank
                vblank();
            };
            t.Start();

            System.Timers.Timer rst8Timer = new System.Timers.Timer(16);
            rst8Timer.AutoReset = true;
            rst8Timer.Elapsed += (source, e) => {
                vblank();
                system.IoController.Interrupt(0, 0xcf); // RST 8 
                Thread.Sleep(8);
                system.IoController.Interrupt(0, 0xd7); // RST 10 start vblank
            };
            //rst8Timer.Enabled = true;
        }

        private MemoryStream loadInvaders() {
            var ms = new MemoryStream();
            using (var fs = File.OpenRead("../../../invaders rom/invaders.h")) {
                fs.CopyTo(ms);
            }
            using (var fs = File.OpenRead("../../../invaders rom/invaders.g")) {
                fs.CopyTo(ms);
            }
            using (var fs = File.OpenRead("../../../invaders rom/invaders.f")) {
                fs.CopyTo(ms);
            }
            using (var fs = File.OpenRead("../../../invaders rom/invaders.e")) {
                fs.CopyTo(ms);
            }
            return ms;
        }
    }
}
