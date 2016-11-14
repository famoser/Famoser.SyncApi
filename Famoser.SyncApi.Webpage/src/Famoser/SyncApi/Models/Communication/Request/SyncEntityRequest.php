<?php
/**
 * Created by PhpStorm.
 * User: famoser
 * Date: 04.11.2016
 * Time: 19:17
 */

namespace Famoser\SyncApi\Models\Communication\Request;


use Famoser\SyncApi\Models\Communication\Entities\SyncCommunicationEntity;
use Famoser\SyncApi\Models\Communication\Request\Base\BaseRequest;

class SyncEntityRequest extends BaseRequest
{
    /* @var SyncCommunicationEntity[] $SyncEntities */
    public $SyncEntities;
}