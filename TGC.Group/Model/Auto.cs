﻿using System;
using System.Collections.Generic;
using Microsoft.DirectX.DirectInput;
using BulletSharp;
using TGC.Core.BulletPhysics;
using TGC.Core.Direct3D;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Geometry;
using TGC.Core.Textures;
using TGC.Core.Particle;
using TGC.Core.Sound;

namespace TGC.Group.Model
{
    public class Auto
    {
        public FisicaMundo Fisica { get; set; }

        //private TgcMesh MayaAuto { get; set; }
        public List<TgcMesh> Mayas { get; set; }
        public RigidBody CuerpoRigidoAuto { get; set; }
        public TGCVector3 PosicionInicial { get; set; }

        //Variables de Mayas de Ruedas
        public List<TgcMesh> Ruedas { get; set; }
        public TgcMesh RuedaDelIzq { get; set; }
        public TgcMesh RuedaDelDer { get; set; }
        public TgcMesh RuedaTrasIzq { get; set; }
        public TgcMesh RuedaTrasDer { get; set; }
        public static TGCVector3 PosicionRuedaDelDer = new TGCVector3(-26, 10.5f, -45f);
        public static TGCVector3 PosicionRuedaDelIzq = new TGCVector3(26, 10.5f, -45f);
        public static TGCVector3 PosicionRuedaTrasDer = new TGCVector3(-26, 10.5f, 44);
        public static TGCVector3 PosicionRuedaTrasIzq = new TGCVector3(26, 10.5f, 44);

        public float GradosRuedaAlDoblar { get; set; }
        public TGCVector3 VersorDirector { get; set; }

        //Cosas de Giros
        public int Direccion { get; set; }
        public TGCVector3 DireccionInicial { get; set; }
        public float GradosRotacion { get; set; }
        public float gradosGiro = FastMath.ToRad(0.4f);

        //Friccion del auto
        public float FriccionAuto { get; set; }

        //Calculo de la Velocidad del Auto
        public float Velocidad
        {
            get => FastMath.Abs(CuerpoRigidoAuto.LinearVelocity.X) + FastMath.Abs(CuerpoRigidoAuto.LinearVelocity.Z) * Direccion;
        }
        public float Velocidad2 { get; set; }

        public readonly float AlturaCuerpoRigido = 20f;

        //Movimiento
        public TGCMatrix Movimiento { get => new TGCMatrix(CuerpoRigidoAuto.InterpolationWorldTransform) * TGCMatrix.Translation(1, -AlturaCuerpoRigido, 1); }

        //Matrices Sombra
        public TGCMatrix MovimientoSombra { get => new TGCMatrix(CuerpoRigidoAuto.InterpolationWorldTransform) * TGCMatrix.Translation(1, -CuerpoRigidoAuto.CenterOfMassPosition.Y + 0.05f, 1); }
        public float EscaladoSombra { get => 1 + (CuerpoRigidoAuto.CenterOfMassPosition.Y - AlturaCuerpoRigido) / 100; }
        public TGCMatrix EscalaSombra { get => TGCMatrix.Scaling(EscaladoSombra, 0, EscaladoSombra); }
        public TGCMatrix MovimientoTotalSombra { get => EscalaSombra * MovimientoSombra; }

        //Matriz que rota las rueda izquierda, para que quede como una rueda derecha
        public TGCMatrix FlipRuedaDerecha = TGCMatrix.RotationZ(FastMath.ToRad(180));

        //Matrices que colocan a las ruedas en su lugar
        public TGCMatrix TraslacionRuedaTrasDer = TGCMatrix.Translation(PosicionRuedaTrasDer);
        public TGCMatrix TraslacionRuedaDelDer = TGCMatrix.Translation(PosicionRuedaDelDer);
        public TGCMatrix TraslacionRuedaTrasIzq = TGCMatrix.Translation(PosicionRuedaTrasIzq);
        public TGCMatrix TraslacionRuedaDelIzq = TGCMatrix.Translation(PosicionRuedaDelIzq);

        //Matriz que hace rotar a las ruedas al doblar
        public TGCMatrix RotarRueda { get => TGCMatrix.RotationY(GradosRuedaAlDoblar * Direccion); }

        //Matrices que hacen girar a las ruedas con la velocidad
        public TGCMatrix GiroAcumuladoIzq = TGCMatrix.Identity;
        public TGCMatrix GiroAcumuladoDer = TGCMatrix.Identity;
        public TGCMatrix GirarRuedaIzq { get => TGCMatrix.RotationX(-Velocidad / 130); }
        public TGCMatrix GirarRuedaDer { get => TGCMatrix.RotationX(Velocidad / 130); }

        //Cosas Sombra
        public TgcPlane PlanoSombra { get; set; }
        public TgcTexture Sombra { get; set; }
        public TgcMesh PlanoSombraMesh { get; set; }

        //Cosas Humo del Auto
        public ParticleEmitter CañoDeEscape1;
        public ParticleEmitter CañoDeEscape2;
        public readonly int CantidadParticulas = 5;
        public TGCVector3 PosicionRelativaCaño1 = new TGCVector3(17, 12, 77);
        public TGCVector3 PosicionRelativaCaño2 = new TGCVector3(-17, 12, 77);

        public void Render(float tiempo)
        {

            foreach (var maya in Mayas)
            {
                maya.AutoTransformEnable = false;
                maya.Transform = Movimiento;
                maya.Render();
            }

            //Matrices que acumulan los cambios
            GiroAcumuladoIzq *= GirarRuedaIzq;
            GiroAcumuladoDer *= GirarRuedaDer;

            RuedaTrasIzq.Transform = GiroAcumuladoIzq * TraslacionRuedaTrasIzq * Movimiento;
            RuedaTrasDer.Transform = GiroAcumuladoDer * FlipRuedaDerecha * TraslacionRuedaTrasDer * Movimiento;
            RuedaDelIzq.Transform = GiroAcumuladoIzq * RotarRueda * TraslacionRuedaDelIzq * Movimiento;
            RuedaDelDer.Transform = GiroAcumuladoDer * FlipRuedaDerecha * RotarRueda * TraslacionRuedaDelDer * Movimiento;
            foreach (var maya in Ruedas)
            {
                maya.Render();
            }

            VersorDirector = TGCVector3.TransformNormal(DireccionInicial, Movimiento);

            //Sombras
            PlanoSombraMesh.Transform = MovimientoTotalSombra;
            PlanoSombraMesh.Render();

            //Humo
            D3DDevice.Instance.ParticlesEnabled = true;
            D3DDevice.Instance.EnableParticles();
            CañoDeEscape1.Position = TGCVector3.TransformCoordinate(PosicionRelativaCaño1, Movimiento);
            CañoDeEscape2.Position = TGCVector3.TransformCoordinate(PosicionRelativaCaño2, Movimiento);
            CañoDeEscape1.render(tiempo);
            CañoDeEscape2.render(tiempo);
        }
    }
}