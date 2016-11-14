<?php
/**
 * Created by PhpStorm.
 * User: famoser
 * Date: 04.11.2016
 * Time: 19:20
 */

namespace Famoser\SyncApi\Models\Communication\Response;


use Famoser\SyncApi\Models\Communication\Entities\CollectionCommunicationEntity;
use Famoser\SyncApi\Models\Communication\Response\Base\BaseResponse;

class CollectionEntityResponse extends BaseResponse
{
    /* @var CollectionCommunicationEntity[] $CollectionEntities */
    public $CollectionEntities;
}