import { DoBackendPost } from "@/helpers/backendFetchHelper";
import { NextApiRequest, NextApiResponse } from "next";

const handler = async (req: NextApiRequest, res: NextApiResponse<number>) => {
    try {
        const apiRes = await DoBackendPost('/api/Albums/CreateNewAlbum', req.body ,req.headers.authorization);
        const data: number = await apiRes.json();
        
        if(!apiRes.ok){
            res.status(apiRes.status).end();
            return;
        }
    
        res.status(200).json(data);
    
    } catch (error) {
        res.status(500).end();
    }
}
    
export default handler;