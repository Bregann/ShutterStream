import { DoBackendPost } from '@/helpers/backendFetchHelper';
import { RegiserUserData } from '@/types/Registration/RegisterUserData';
import type { NextApiRequest, NextApiResponse } from 'next'

 const handler = async (req: NextApiRequest, res: NextApiResponse<RegiserUserData>) => {
    try {
        const apiRes = await DoBackendPost('/api/Auth/RegisterNewUser', req.body);

        if(!apiRes.ok){
            res.status(apiRes.status).json({success: false, reason: apiRes.statusText})
            return;
        }

        const apiData: RegiserUserData = await apiRes.json();
        
        res.status(200).json(apiData);

    } catch (error) {
        res.status(500).json({success: false, reason: "An error has occurred, please try again shortly."})
    }
  }

  export default handler;