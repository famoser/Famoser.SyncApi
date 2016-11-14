<?php
/**
 * Created by PhpStorm.
 * User: famoser
 * Date: 04.11.2016
 * Time: 19:15
 */

namespace Famoser\SyncApi\Models\Communication\Request;


use Famoser\SyncApi\Models\Communication\Entities\CollectionCommunicationEntity;
use Famoser\SyncApi\Models\Communication\Request\Base\BaseRequest;

class CollectionEntityRequest extends BaseRequest
{
    /* @var CollectionCommunicationEntity[] $CollectionEntities */
    public $CollectionEntities;
}