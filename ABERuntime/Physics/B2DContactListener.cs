using System;
using System.Security.Cryptography;
using Box2D.NetStandard.Collision;
using Box2D.NetStandard.Common;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Contacts;
using Box2D.NetStandard.Dynamics.Fixtures;
using Box2D.NetStandard.Dynamics.World;
using Box2D.NetStandard.Dynamics.World.Callbacks; 
using ABEngine.ABERuntime.ECS;
using SixLabors.ImageSharp.ColorSpaces;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABERuntime.Physics
{
    internal class B2DContactListener : ContactListener
    {

        public override void BeginContact(in Contact contact)
        {
            if (contact.IsEnabled())
            {
                var rbA = contact.GetFixtureA().GetBody().GetUserData<Rigidbody>();
                var rbB = contact.GetFixtureB().GetBody().GetUserData<Rigidbody>();

                CollisionData collision = new CollisionData()
                {
                    collisionType = CollisionType.Enter,
                    rigidbodyA = rbA,
                    rigidbodyB = rbB
                };
                PhysicsManager.RegisterCollision(collision);

                //rbA.CollisionEnter(rbB);
                //rbB.CollisionEnter(rbA);
            }
        }

        public override void EndContact(in Contact contact)
        {
            if (contact.IsEnabled())
            {
                var rbA = contact.GetFixtureA().GetBody().GetUserData<Rigidbody>();
                var rbB = contact.GetFixtureB().GetBody().GetUserData<Rigidbody>();


                CollisionData collision = new CollisionData()
                {
                    collisionType = CollisionType.Exit,
                    rigidbodyA = rbA,
                    rigidbodyB = rbB
                };
                PhysicsManager.RegisterCollision(collision);


                //rbA.CollisionExit(rbB);
                //rbB.CollisionExit(rbA);
            }

            //contact.SetEnabled(true);
        }

        public override void PostSolve(in Contact contact, in ContactImpulse impulse)
        {
            //throw new NotImplementedException();
        }

        public override void PreSolve(in Contact contact, in Manifold oldManifold)
        {
            Fixture fixtureA = contact.GetFixtureA();
            Fixture fixtureB = contact.GetFixtureB();

            Rigidbody rbA = fixtureA.GetBody().GetUserData<Rigidbody>();
            Rigidbody rbB = fixtureB.GetBody().GetUserData<Rigidbody>();

            float normalMult = 1;

            //check if one of the fixtures is the platform
            Fixture platformFixture = null;
            Fixture otherFixture = null;
            if (rbA.entity.Has<Platform2D>())
            {
                platformFixture = fixtureA;
                otherFixture = fixtureB;
            }
            else if (rbB.entity.Has<Platform2D>())
            {
                platformFixture = fixtureB;
                otherFixture = fixtureA;
                normalMult = -1;
            }

            if(platformFixture != null) // Has platform
            {
                Body platformBody = platformFixture.GetBody();
                Body otherBody = otherFixture.GetBody();


                contact.GetWorldManifold(out WorldManifold worldManifold);
                if (worldManifold.normal.Y * normalMult < -0.5f)
                {
                    contact.SetEnabled(false);
                }

                //no points are moving downward, contact should not be solid
                //contact.SetEnabled(false);
            }
        }
    }
}
