<?php
/**
 * Created by PhpStorm.
 * User: famoser
 * Date: 04.11.2016
 * Time: 19:03
 */

namespace Famoser\SyncApi\Controllers;


use Famoser\SyncApi\Controllers\Base\ApiRequestController;
use Famoser\SyncApi\Controllers\Base\BaseController;
use Famoser\SyncApi\Exceptions\ServerException;
use Famoser\SyncApi\Helpers\RequestHelper;
use Famoser\SyncApi\Models\Communication\Entities\CollectionEntity;
use Famoser\SyncApi\Models\Communication\Request\Base\BaseRequest;
use Famoser\SyncApi\Models\Communication\Response\CollectionEntityResponse;
use Famoser\SyncApi\Models\Entities\Collection;
use Famoser\SyncApi\Models\Entities\ContentVersion;
use Famoser\SyncApi\Models\Entities\UserCollection;
use Famoser\SyncApi\Types\ContentType;
use Famoser\SyncApi\Types\OnlineAction;
use Famoser\SyncApi\Types\ServerError;
use Slim\Http\Request;
use Slim\Http\Response;

class CollectionController extends ApiRequestController
{
    public function sync(Request $request, Response $response, $args)
    {
        $req = RequestHelper::parseCollectionEntityRequest($request);
        $this->authorizeRequest($req);
        $this->authenticateRequest($req);

        $resp = new CollectionEntityResponse();
        foreach ($req->CollectionEntities as $collectionEntity) {
            $entity = $collectionEntity;
            if ($entity->OnlineAction == OnlineAction::Create) {
                $coll = new Collection();
                $coll->user_guid = $this->getUser($req)->guid;
                $coll->device_guid = $this->getDevice($req)->guid;
                $coll->guid = $entity->Id;
                $coll->identifier = $entity->Identifier;

                if (!$this->getDatabaseHelper()->saveToDatabase($coll))
                    throw new ServerException(ServerError::DatabaseSaveFailure);

                $content = ContentVersion::createNewForCollection($entity);
                if (!$this->getDatabaseHelper()->saveToDatabase($content))
                    throw new ServerException(ServerError::DatabaseSaveFailure);
            } else if ($entity->OnlineAction == OnlineAction::Update) {
                $coll = $this->getCollectionById($req, $entity->Id);
                if ($coll != null) {
                    $ver = $this->getActiveVersion($coll->guid);

                    $ce = new CollectionEntity();
                    $ce->Id = $coll->guid;
                    $ver->writeToEntity($ce);
                    $ce->DeviceId = $coll->device_guid;
                    $ce->UserId = $coll->user_guid;
                    //todo: continue. refactor writeToEntity as it is ambiguous?
                }
            }
        }
    }

    private $tempCollections;

    /**
     * @param BaseRequest $req
     * @param $guid
     * @return Collection
     * @throws \Famoser\SyncApi\Exceptions\ApiException
     */
    private function getCollectionById(BaseRequest $req, $guid)
    {
        if ($this->tempCollections != null)
            return in_array($guid, $this->tempCollections) ? $this->tempCollections[$guid] : null;

        //get all accessible collection guids
        $db = $this->getDatabaseHelper();
        $userCollections = $db->getFromDatabase(new UserCollection(), "user_guid =:user_guid", array("user_guid" => $this->getUser($req)->guid), null, 1000, "collection_guid");
        $collectionIds = [];
        foreach ($userCollections as $co) {
            $collectionIds[] = $co->collection_guid;
        }

        //get all collections
        $collections = $db->getFromDatabase(new Collection(), "guid IN (" . implode(',:', array_keys($collectionIds)) . ")", $collectionIds);

        //save them in temp collections
        $this->tempCollections = array();
        foreach ($collections as $collection) {
            $this->tempCollections[$collection->guid] = $collection;
        }

        //recursively call to return
        return $this->getCollectionById($req, $guid);
    }

    /**
     * @param $guid
     * @return bool|ContentVersion
     */
    private function getActiveVersion($guid)
    {
        return $this->getDatabaseHelper()->getSingleFromDatabase(new ContentVersion(), "content_type = :content_type AND entity_guid = :entity_guid", array("content_type" => ContentType::Collection, "entity_guid" => $guid), "create_date_time DESC");
    }
}