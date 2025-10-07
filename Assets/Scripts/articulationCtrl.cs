using UnityEngine;

public class articulationCtrl : MonoBehaviour
{
    public ArticulationBody[] articulationBodies;

    // 定期的に呼ばれる
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.O))
        {
            var xDrive = this.articulationBodies[7].xDrive;
            xDrive.target -= 1f;
            this.articulationBodies[7].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.L))
        {
            var xDrive = this.articulationBodies[7].xDrive;
            xDrive.target += 1f;
            this.articulationBodies[7].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.I))
        {
            var xDrive = this.articulationBodies[6].xDrive;
            xDrive.target -= 1f;
            this.articulationBodies[6].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.K))
        {
            var xDrive = this.articulationBodies[6].xDrive;
            xDrive.target += 1f;
            this.articulationBodies[6].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.U))
        {
            var xDrive = this.articulationBodies[5].xDrive;
            xDrive.target -= 1f;
            this.articulationBodies[5].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.J))
        {
            var xDrive = this.articulationBodies[5].xDrive;
            xDrive.target += 1f;
            this.articulationBodies[5].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.Y))
        {
            var xDrive = this.articulationBodies[4].xDrive;
            xDrive.target -= 1f;
            this.articulationBodies[4].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.H))
        {
            var xDrive = this.articulationBodies[4].xDrive;
            xDrive.target += 1f;
            this.articulationBodies[4].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.Z))
        {
            var xDrive = this.articulationBodies[3].xDrive;
            xDrive.target -= 1f;
            this.articulationBodies[3].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.X))
        {
            var xDrive = this.articulationBodies[3].xDrive;
            xDrive.target += 1f;
            this.articulationBodies[3].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.C))
        {
            var xDrive = this.articulationBodies[2].xDrive;
            xDrive.target -= 1f;
            this.articulationBodies[2].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.V))
        {
            var xDrive = this.articulationBodies[2].xDrive;
            xDrive.target += 1f;
            this.articulationBodies[2].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.B))
        {
            var xDrive = this.articulationBodies[1].xDrive;
            xDrive.target -= 1f;
            this.articulationBodies[1].xDrive = xDrive;
        }
        else if (Input.GetKey(KeyCode.N))
        {
            var xDrive = this.articulationBodies[1].xDrive;
            xDrive.target += 1f;
            this.articulationBodies[1].xDrive = xDrive;
        }

        //crawler
        else if (Input.GetKey(KeyCode.R))
        {
            for (int i = 8; i < 14; i++)
            {
                var xDrive = this.articulationBodies[i].xDrive;
                xDrive.target += 10f;
                this.articulationBodies[i].xDrive = xDrive;
            }
        }
        else if (Input.GetKey(KeyCode.F))
        {
            for (int i = 8; i < 14; i++)
            {
                var xDrive = this.articulationBodies[i].xDrive;
                xDrive.target -= 10f;
                this.articulationBodies[i].xDrive = xDrive;
            }
        }
        else if (Input.GetKey(KeyCode.T))
        {
            for (int i = 14; i < 20; i++)
            {
                var xDrive = this.articulationBodies[i].xDrive;
                xDrive.target += 10f;
                this.articulationBodies[i].xDrive = xDrive;
            }
        }
        else if (Input.GetKey(KeyCode.G))
        {
            for (int i = 14; i < 20; i++)
            {
                var xDrive = this.articulationBodies[i].xDrive;
                xDrive.target -= 10f;
                this.articulationBodies[i].xDrive = xDrive;
            }
        }
    }
}